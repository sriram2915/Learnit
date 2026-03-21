export default Signup;
import { useState } from "react";
import { FaEye, FaEyeSlash } from "react-icons/fa";
import { Link, useNavigate } from "react-router-dom";
import { authApi } from "../../services";
import Button from "../ui/Button";
import styles from "./AuthBase.module.css";

function Signup() {
  const [form, setForm] = useState({
    fullName: "",
    email: "",
    password: "",
    confirmPassword: "",
  });
  const [showPassword, setShowPassword] = useState({
    password: false,
    confirmPassword: false,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const navigate = useNavigate();

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.fullName.trim()) return setError("Full name is required");
    if (!form.email.trim()) return setError("Email is required");
    if (form.password.length < 6)
      return setError("Password must be at least 6 characters");
    if (form.password !== form.confirmPassword)
      return setError("Passwords do not match");

    setLoading(true);
    setError("");
    try {
      const response = await authApi.register(
        form.fullName,
        form.email,
        form.password
      );

      // Server returns { message: "Registered successfully" } on success
      // Check if registration was successful
      if (
        response &&
        response.message &&
        response.message.includes("success")
      ) {
        // Redirect to login page after successful registration
        navigate("/auth/login", { replace: true });
        return;
      }

      // If response doesn't have expected success message
      throw new Error("Registration completed but confirmation was unclear");
    } catch (err) {
      // Extract error message from server response
      const errorMessage =
        err.message ||
        err.data?.message ||
        "Registration failed. Please try again.";
      setError(errorMessage);
    }
  };

  return (
    <div className={styles.authLayout}>
      <div className={styles.authBrandPane}>
        <span className={styles.authKicker}>Learnit</span>
        <h1>Get started</h1>
        <p className={styles.authCopy}>
          Create your free account to start learning, track your progress, and
          join classrooms.
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
              <span className={styles.authEyebrow}>Sign up</span>
              <h2>Create your account</h2>
            </div>
          </div>
          {error && <div className={styles.authError}>{error}</div>}
          <div className={styles.authForm}>
            <div className={styles.authField}>
              <span>Full name</span>
              <input
                name="fullName"
                type="text"
                placeholder="Jordan Lee"
                autoComplete="name"
                value={form.fullName}
                onChange={handleChange}
                disabled={loading}
                required
              />
            </div>
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
                  type={showPassword.password ? "text" : "password"}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  minLength={6}
                  value={form.password}
                  onChange={handleChange}
                  disabled={loading}
                  required
                />
                <button
                  type="button"
                  aria-label={
                    showPassword.password ? "Hide password" : "Show password"
                  }
                  onClick={() =>
                    setShowPassword((v) => ({ ...v, password: !v.password }))
                  }
                  className={styles.authPasswordToggle}
                  tabIndex={-1}
                >
                  {showPassword.password ? (
                    <FaEyeSlash size={18} />
                  ) : (
                    <FaEye size={18} />
                  )}
                </button>
              </div>
            </div>
            <div className={styles.authField}>
              <span>Confirm password</span>
              <div className={styles.authPasswordWrapper}>
                <input
                  name="confirmPassword"
                  className={styles.authPasswordInput}
                  type={showPassword.confirmPassword ? "text" : "password"}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  minLength={6}
                  value={form.confirmPassword}
                  onChange={handleChange}
                  disabled={loading}
                  required
                />
                <button
                  type="button"
                  aria-label={
                    showPassword.confirmPassword
                      ? "Hide password"
                      : "Show password"
                  }
                  onClick={() =>
                    setShowPassword((v) => ({
                      ...v,
                      confirmPassword: !v.confirmPassword,
                    }))
                  }
                  className={styles.authPasswordToggle}
                  tabIndex={-1}
                >
                  {showPassword.confirmPassword ? (
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
            Sign up
          </Button>
          <div className={styles.authFooterText}>
            Already have an account? <Link to="/auth/login">Sign in</Link>
          </div>
        </form>
      </div>
    </div>
  );
}
