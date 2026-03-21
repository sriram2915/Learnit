import React from "react";
import styles from "../Schedule.module.css";

export function ScheduleHeader({ onAutoSchedule, onReset, loading }) {
  return (
    <div className={styles.pageHeader}>
      <div className={styles.controls}>
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
