import styles from "../main/Schedule.module.css";

export function ScheduleHeader({
  onToday,
  onAutoSchedule,
  onReset,
  loading,
  productivityScore,
}) {
  return (
    <div className={styles.pageHeader}>
      <div>
        <p className={styles.kicker}>AI-powered scheduling</p>
        <h1>Study planner</h1>
        <p className={styles.subtle}>
          Intelligent time blocking for optimal learning outcomes
        </p>
      </div>

      <div className={styles.controls}>
        <div className={styles.productivityBadge}>
          <span>Productivity Score</span>
          <strong>{productivityScore}%</strong>
        </div>

        <button className={styles.lightBtn} onClick={onToday} type="button">
          Today
        </button>

        <button
          className={styles.primaryBtn}
          type="button"
          onClick={onAutoSchedule}
          disabled={loading}
        >
          🚀 Auto-schedule modules
        </button>

        <button
          className={styles.secondaryBtn}
          type="button"
          onClick={onReset}
          disabled={loading}
        >
          🧹 Reset schedule
        </button>
      </div>
    </div>
  );
}
