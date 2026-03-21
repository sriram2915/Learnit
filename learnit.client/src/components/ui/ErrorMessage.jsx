import { useState } from "react";
import { FiAlertCircle, FiRefreshCw, FiX } from "react-icons/fi";
import styles from "./ErrorMessage.module.css";

/**
 * ErrorMessage Component
 * Displays error messages with retry functionality
 */
export function ErrorMessage({
  error,
  onRetry,
  onDismiss,
  title = "Something went wrong",
  showIcon = true,
  variant = "default", // 'default', 'inline', 'banner'
}) {
  const [isRetrying, setIsRetrying] = useState(false);

  if (!error) return null;

  const errorMessage =
    typeof error === "string" ? error : error.message || "An unexpected error occurred";

  const handleRetry = async () => {
    if (!onRetry || isRetrying) return;
    setIsRetrying(true);
    try {
      await onRetry();
    } finally {
      setIsRetrying(false);
    }
  };

  const containerClass = `${styles.container} ${styles[variant]}`;

  return (
    <div className={containerClass} role="alert">
      <div className={styles.content}>
        {showIcon && (
          <div className={styles.icon}>
            <FiAlertCircle />
          </div>
        )}
        <div className={styles.text}>
          <strong className={styles.title}>{title}</strong>
          <p className={styles.message}>{errorMessage}</p>
        </div>
        {onDismiss && (
          <button
            className={styles.dismissBtn}
            onClick={onDismiss}
            aria-label="Dismiss error"
          >
            <FiX />
          </button>
        )}
      </div>
      {onRetry && (
        <button
          className={styles.retryBtn}
          onClick={handleRetry}
          disabled={isRetrying}
        >
          <FiRefreshCw className={isRetrying ? styles.spinning : ""} />
          {isRetrying ? "Retrying..." : "Retry"}
        </button>
      )}
    </div>
  );
}

/**
 * Inline Error - for form fields
 */
export function InlineError({ error, className = "" }) {
  if (!error) return null;
  return (
    <div className={`${styles.inline} ${className}`} role="alert">
      <FiAlertCircle className={styles.inlineIcon} />
      <span>{typeof error === "string" ? error : error.message}</span>
    </div>
  );
}

export default ErrorMessage;

