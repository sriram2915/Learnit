import styles from "./Loading.module.css";

/**
 * Loading Spinner Component
 * Reusable loading indicator with different sizes
 */
export function Loading({ size = "medium", fullScreen = false, message = "" }) {
  const sizeClass = styles[size] || styles.medium;
  const containerClass = fullScreen ? styles.fullScreen : styles.inline;

  return (
    <div className={containerClass}>
      <div className={styles.spinnerContainer}>
        <div className={`${styles.spinner} ${sizeClass}`}></div>
        {message && <p className={styles.message}>{message}</p>}
      </div>
    </div>
  );
}

/**
 * Inline Loading - for buttons and small areas
 */
export function InlineLoading({ size = "small" }) {
  return <div className={`${styles.spinner} ${styles[size]}`}></div>;
}

export default Loading;

