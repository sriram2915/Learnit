/**
 * HTTP Client - Core request handler
 * Handles all HTTP communication with error handling and auth
 */

// In dev, Vite proxies /api to the backend, so the base URL can be empty.
// In prod (S3 static hosting), you must set VITE_API_BASE_URL to your API origin
// (e.g. https://<env>.elasticbeanstalk.com).
const API_BASE_URL = (import.meta?.env?.VITE_API_BASE_URL ?? "").trim();

const normalizeBaseUrl = (value) => {
  if (!value) return "";
  return value.endsWith("/") ? value.slice(0, -1) : value;
};

class HttpClient {
  constructor(baseURL = API_BASE_URL) {
    this.baseURL = normalizeBaseUrl(baseURL);
  }

  async request(endpoint, options = {}) {
    const isAbsolute = /^https?:\/\//i.test(endpoint);
    const url = isAbsolute
      ? endpoint
      : this.baseURL
      ? `${this.baseURL}${endpoint}`
      : endpoint;
    const token = localStorage.getItem("token");

    const config = {
      ...options,
      headers: {
        "Content-Type": "application/json",
        ...(token && { Authorization: `Bearer ${token}` }),
        ...options.headers,
      },
    };

    try {
      if (import.meta.env.DEV) {
        console.log(`[HTTP ${config.method}] ${url}`);
      }

      const response = await fetch(url, config);

      // Check if response is JSON
      const contentType = response.headers.get("content-type");
      let data;

      if (contentType?.includes("application/json")) {
        data = await response.json();
      } else {
        data = await response.text();
      }

      if (import.meta.env.DEV) {
        console.log(`[HTTP Response] Status: ${response.status}`, data);
      }

      if (!response.ok) {
        const errorMessage =
          (typeof data === "object" && data?.message) ||
          (typeof data === "object" && data?.error) ||
          data ||
          `HTTP ${response.status}`;
        const error = new Error(errorMessage);
        error.status = response.status;
        error.data = data;
        throw error;
      }

      return data;
    } catch (error) {
      if (import.meta.env.DEV) {
        console.error("API request failed:", {
          url,
          method: config.method,
          status: error.status,
          message: error.message,
          data: error.data,
        });
      }
      throw error;
    }
  }

  async get(endpoint) {
    return this.request(endpoint, { method: "GET" });
  }

  async post(endpoint, data) {
    return this.request(endpoint, {
      method: "POST",
      body: JSON.stringify(data),
    });
  }

  async put(endpoint, data) {
    return this.request(endpoint, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  }

  async patch(endpoint, data) {
    return this.request(endpoint, {
      method: "PATCH",
      body: JSON.stringify(data),
    });
  }

  async delete(endpoint) {
    return this.request(endpoint, { method: "DELETE" });
  }
}

export default new HttpClient();
