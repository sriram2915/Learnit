import React from "react";
import styles from "../Schedule.module.css";

export function QuickActions() {
  return (
    <div className={styles.quickActions}>
      <button className={styles.quickBtn} type="button">
        📊 Generate progress report
      </button>
      <button className={styles.quickBtn} type="button">
        ⚡ Optimize for energy levels
      </button>
      <button className={styles.quickBtn} type="button">
        🎯 Adjust learning goals
      </button>
    </div>
  );
}
