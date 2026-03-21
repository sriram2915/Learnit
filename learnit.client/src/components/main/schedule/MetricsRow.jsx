import React from "react";
import styles from "../Schedule.module.css";

export function MetricsRow({ weeklyGoal, completedThisWeek, loading = false }) {
  const goalLabel = loading ? "Loading..." : weeklyGoal;
  const completedLabel = loading ? "Loading..." : completedThisWeek;

  return (
    <div className={styles.metricsRow}>
      <div className={styles.metricCard}>
        <span>Weekly Goal</span>
        <strong>{goalLabel}</strong>
      </div>
      <div className={styles.metricCard}>
        <span>Completed</span>
        <strong>{completedLabel}</strong>
      </div>
    </div>
  );
}
