# Deploy Learnit to AWS (S3 + Elastic Beanstalk + RDS)

Target architecture:

- React (Vite) SPA → S3 static hosting
- ASP.NET Core Web API → Elastic Beanstalk (.NET on Linux)
- PostgreSQL → Amazon RDS
- Secrets (JWT, AI keys) → Elastic Beanstalk environment variables

This repo is already structured as a SPA calling a backend at `/api/*`.
In production, S3 cannot proxy `/api`, so the client must call the API by full origin.

## 0) One-time prerequisites

- AWS account (admin IAM identity already available)
- AWS region chosen (keep everything in the same region)
- Locally installed:
  - Node.js (LTS)
  - .NET SDK (the repo targets .NET 9)
  - (Optional) AWS CLI v2

## 1) Create PostgreSQL in Amazon RDS

1. RDS → Create database
2. Engine: PostgreSQL
3. Template: Production (or Dev/Test if you prefer)
4. Set:
   - DB instance identifier (e.g. `learnit-prod`)
   - Master username + password
   - Initial database name (e.g. `learnit`)
5. Networking:
   - VPC: same VPC where Elastic Beanstalk will run
   - Public access: **No** (recommended)
   - Security group: create one (e.g. `learnit-rds-sg`)
6. After creation, edit the RDS security group inbound rules:
   - Allow inbound TCP 5432 **from the Elastic Beanstalk EC2 security group** (not from the internet)

Connection string format you’ll use later:

`Host=<rds-endpoint>;Port=5432;Database=learnit;Username=<user>;Password=<pwd>;SSL Mode=Require;Trust Server Certificate=true`

## 2) Deploy ASP.NET Core API to Elastic Beanstalk

### 2.1 Create Elastic Beanstalk environment

1. Elastic Beanstalk → Create application (e.g. `learnit-api`)
2. Platform:
   - .NET on Linux (use the newest available matching your runtime)
3. Environment:
   - Load balanced (recommended)

### 2.2 Configure environment variables (secrets + config)

Elastic Beanstalk → Environment → Configuration → Software → Environment properties:

Required:

- `ConnectionStrings__Default` = your RDS connection string
- `Jwt__Key` = a long random secret (at least 32 chars)
- `Jwt__Issuer` = `Learnit` (or your domain)
- `Jwt__Audience` = `LearnitUsers` (or your domain)

CORS (set at least one):

- `Cors__AllowedOrigins__0` = `http://<your-s3-website-hostname>` OR `https://<your-cloudfront-domain>`

AI keys (optional but recommended):

- `Groq__ApiKey` (or `GROQ_API_KEY`)
- `Groq__Model` (optional)
- `OpenAi__ApiKey` (or `OPENAI_API_KEY`)

First deploy only (optional):

- `Database__RunMigrations` = `true`

After the DB schema exists, set it back to `false` (recommended) and handle migrations explicitly.

### 2.3 Add a health check path

In EB, set the environment health check URL to:

- `/health`

### 2.4 Build & deploy the API

From `Learnit.Server/` locally:

1. Publish release output:

`dotnet publish -c Release -o publish`

2. Zip the contents of the `publish/` folder (zip the _contents_, not the folder itself)
3. Upload the zip in Elastic Beanstalk → Application versions → Upload
4. Deploy that version to your environment

Confirm API is reachable:

- `https://<your-eb-env>.elasticbeanstalk.com/health`

## 3) Deploy React (Vite) client to S3

### 3.1 Build the SPA with the correct API base URL

Because S3 can’t proxy `/api`, set the API origin at build time.

In `learnit.client/`, create `.env.production` (do not commit) based on `.env.production.example`:

`VITE_API_BASE_URL=https://<your-eb-env>.elasticbeanstalk.com`

Then build:

`npm ci`
`npm run build`

This produces `learnit.client/dist/`.

### 3.2 Create S3 bucket and enable static hosting

1. S3 → Create bucket (e.g. `learnit-prod-web`)
2. Properties → Static website hosting:
   - Enable
   - Index document: `index.html`
   - Error document: `index.html` (important for React Router)
3. Permissions:
   - Simplest: allow public read (for a quick launch)
   - Recommended: CloudFront + Origin Access Control (more secure)

### 3.3 Upload the build output

Upload all files inside `dist/` to the bucket root.

Verify:

- Open the S3 website endpoint and ensure the landing page loads.
- Test a deep link route directly (e.g. `/auth/login`) → should still return the SPA.

## 4) Final production checklist (app-specific)

- CORS:
  - Ensure `Cors__AllowedOrigins__0` matches your deployed frontend origin exactly.
- JWT:
  - Use a strong `Jwt__Key`.
  - Keep issuer/audience consistent.
- Database:
  - Run `Database__RunMigrations=true` only for initial bootstrap (or when you intend to migrate on startup).
- AI:
  - If keys are missing, the app still works but AI replies are stubbed.
- Friends feature:
  - Current implementation stores friend lists in-memory on the API instance. In a multi-instance / restart scenario, friend lists will reset.
  - If you want this truly production-grade, we should persist friends in PostgreSQL (EF migration + model).

## 5) What changed in the code to support this

- Client now supports `VITE_API_BASE_URL` for production API calls.
- Server CORS origins are now configurable via `Cors:AllowedOrigins`.
- Server adds `/health` for EB health checks.
- Server supports `Database:RunMigrations` (optional startup migration).
