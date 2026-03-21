import React from "react";
import styles from "../Schedule.module.css";

export function ResetScheduleModal({
  isOpen,
  error,
  onClose,
  onConfirm,
  loading,
}) {
  if (!isOpen) return null;

  return (
    <div className={styles.modalOverlay}>
      <div className={styles.modal}>
        <div className={styles.modalHeader}>
          <div>
            <p className={styles.kicker}>Reset</p>
            <h2>Clear all scheduled events?</h2>
            <p className={styles.subtle}>
              This removes every event from your calendar. Linked modules remain
              untouched.
            </p>
          </div>
          <button className={styles.iconBtn} type="button" onClick={onClose}>
            ×
          </button>
        </div>

        {error && <div className={styles.errorMessage}>{error}</div>}

        <div className={styles.formActions}>
          <button
            className={styles.secondaryBtn}
            type="button"
            onClick={onClose}
          >
            Cancel
          </button>
          <button
            className={styles.dangerBtn}
            type="button"
            onClick={onConfirm}
            disabled={loading}
          >
            Clear all events
          </button>
        </div>
      </div>
    </div>
  );
}
