import styles from "../main/Schedule.module.css";

export function ScheduleMetrics({
  weeklyGoal,
  completedThisWeek,
  productivityScore,
}) {
  return (
    <div className={styles.metricsRow}>
      <div className={styles.metricCard}>
        <span>Weekly Goal</span>
        <strong>{weeklyGoal}</strong>
      </div>
      <div className={styles.metricCard}>
        <span>Completed</span>
        <strong>{completedThisWeek}</strong>
      </div>
      <div className={styles.metricCard}>
        <span>Focus Score</span>
        <strong>{productivityScore}%</strong>
      </div>
      <div className={styles.metricCard}>
        <span>AI Confidence</span>
        <strong>92%</strong>
      </div>
    </div>
  );
}
