import { useState, useContext } from "react";
import { FaEye, FaEyeSlash } from "react-icons/fa";
import { Link, useNavigate } from "react-router-dom";
import { authApi } from "../../services";
import { AuthContext } from "../../context/AuthContext";
import Button from "../ui/Button";
import styles from "./AuthBase.module.css";

function Signin() {
  const [form, setForm] = useState({ email: "", password: "" });
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const { login } = useContext(AuthContext);
  const navigate = useNavigate();

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const applySession = (token) => {
    localStorage.setItem("token", token);
    const baseUser = (() => {
      try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        return { id: payload.sub, email: payload.email };
      } catch (err) {
        console.error("Failed to parse token payload: ", err.message);
        return {};
      }
    })();
    login({ ...baseUser, token });
    navigate("/app/course", { replace: true });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.email.trim() || !form.password.trim()) {
      setError("Email and password are required");
      return;
    }

    setLoading(true);
    setError("");
    try {
      const { token } = await authApi.login(form.email, form.password);
      if (!token) throw new Error("No token received from server");
      applySession(token);
    } catch (err) {
      setError(err.message || "Login failed. Please try again.");
    }
  };

  return (
    <div className={styles.authLayout}>
      <div className={styles.authBrandPane}>
        <span className={styles.authKicker}>Learnit</span>
        <h1>Welcome back</h1>
        <p className={styles.authCopy}>
          Sign in to access your courses, schedule, and progress dashboard.
        </p>
      </div>
      <div className={styles.authCardPane}>
        <form
          className={styles.authCard}
          onSubmit={handleSubmit}
          autoComplete="on"
        >
          <div className={styles.authHeader}>
            <div>
              <span className={styles.authEyebrow}>Sign in</span>
              <h2>Sign in to your account</h2>
            </div>
          </div>
          <p className={styles.authSubhead}>
            Enter your email and password below.
          </p>
          {error && <div className={styles.authError}>{error}</div>}
          <div className={styles.authForm}>
            <div className={styles.authField}>
              <span>Email address</span>
              <input
                name="email"
                type="email"
                placeholder="you@example.com"
                autoComplete="email"
                value={form.email}
                onChange={handleChange}
                disabled={loading}
                required
              />
            </div>
            <div className={styles.authField}>
              <span>Password</span>
              <div className={styles.authPasswordWrapper}>
                <input
                  name="password"
                  className={styles.authPasswordInput}
                  type={showPassword ? "text" : "password"}
                  placeholder="••••••••"
                  autoComplete="current-password"
                  value={form.password}
                  onChange={handleChange}
                  disabled={loading}
                  required
                />
                <button
                  type="button"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  onClick={() => setShowPassword((v) => !v)}
                  className={styles.authPasswordToggle}
                  tabIndex={-1}
                >
                  {showPassword ? (
                    <FaEyeSlash size={18} />
                  ) : (
                    <FaEye size={18} />
                  )}
                </button>
              </div>
            </div>
          </div>
          <Button
            type="submit"
            className={styles.authButton}
            loading={loading}
            variant="primary"
          >
            Sign in
          </Button>
          <div className={styles.authFooterText}>
            Don&apos;t have an account?{" "}
            <Link className={styles.authInlineLink} to="/auth/register">
              Sign up
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}

export default Signin;
