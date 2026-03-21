import styles from "./Loading.module.css";

/**
 * Skeleton Loader Components
 * For better loading UX
 */

export function SkeletonText({ lines = 3, className = "" }) {
  return (
    <div className={className}>
      {Array.from({ length: lines }).map((_, i) => (
        <div
          key={i}
          className={`${styles.skeleton} ${styles.skeletonText}`}
          style={{ width: i === lines - 1 ? "80%" : "100%" }}
        />
      ))}
    </div>
  );
}

export function SkeletonTitle({ className = "" }) {
  return (
    <div
      className={`${styles.skeleton} ${styles.skeletonTitle} ${className}`}
    />
  );
}

export function SkeletonCard({ className = "" }) {
  return (
    <div
      className={`${styles.skeleton} ${styles.skeletonCard} ${className}`}
    />
  );
}

export function SkeletonButton({ className = "" }) {
  return (
    <div
      className={`${styles.skeleton} ${styles.skeletonButton} ${className}`}
    />
  );
}

/**
 * Course Card Skeleton
 */
export function CourseCardSkeleton() {
  return (
    <div
      style={{
        background: "var(--card-bg, #fff)",
        borderRadius: "8px",
        padding: "1.5rem",
        border: "1px solid var(--border-color, #e2e8f0)",
      }}
    >
      <SkeletonTitle />
      <SkeletonText lines={2} style={{ marginTop: "1rem" }} />
      <div style={{ marginTop: "1rem", display: "flex", gap: "0.5rem" }}>
        <SkeletonButton />
        <SkeletonButton />
      </div>
    </div>
  );
}

export default SkeletonText;

