import styles from "./ProgressCard.module.css";

function ProgressCard({
  progressPercentage,
  completedModules,
  totalModules,
  totalHours,
  completedHours,
  hoursRemaining,
}) {
  const remaining = hoursRemaining ?? 0;
  const pct = progressPercentage ?? null;

  const effectiveTotalHours = (() => {
    if (totalHours && totalHours > 0) return totalHours;
    if (pct && pct > 0 && pct < 100) {
      const denom = 1 - pct / 100;
      return denom > 0 ? Math.round((remaining / denom) * 10) / 10 : remaining;
    }
    return remaining;
  })();

  const derivedProgress =
    pct !== null && pct !== undefined
      ? pct
      : effectiveTotalHours > 0
      ? ((effectiveTotalHours - remaining) / effectiveTotalHours) * 100
      : 0;

  const hoursDone =
    completedHours && completedHours > 0
      ? completedHours
      : Math.max(
          0,
          Math.round(((totalHours ?? effectiveTotalHours) - remaining) * 10) /
            10
        );

  return (
    <div className={styles.card}>
      <div className={styles.stats}>
        <div className={styles.stat}>
          <div className={styles.value}>{Math.round(derivedProgress)}%</div>
          <div className={styles.label}>Complete</div>
        </div>
        <div className={styles.stat}>
          <div className={styles.value}>{hoursDone}</div>
          <div className={styles.label}>Hours Done</div>
        </div>
        <div className={styles.stat}>
          <div className={styles.value}>{hoursRemaining}</div>
          <div className={styles.label}>Hours Left</div>
        </div>
      </div>

      {typeof totalModules === "number" && (
        <div className={styles.modulesRow}>
          <span className={styles.modulesLabel}>Modules</span>
          <span className={styles.modulesValue}>
            {completedModules ?? 0}/{totalModules}
          </span>
        </div>
      )}

      <div className={styles.barContainer}>
        <div className={styles.bar}>
          <div
            className={styles.fill}
            style={{ width: `${Math.min(100, derivedProgress)}%` }}
          />
        </div>
        <div className={styles.text}>
          <span className={styles.percent}>{Math.round(derivedProgress)}%</span>
          <span className={styles.details}>{hoursRemaining}h remaining</span>
        </div>
      </div>
    </div>
  );
}

export default ProgressCard;
