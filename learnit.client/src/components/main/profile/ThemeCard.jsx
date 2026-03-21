import React from "react";
import styles from "../Profile.module.css";

export function ThemeCard({ isDarkMode, onToggle }) {
  return (
    <section className={styles.card}>
      <div className={styles.cardHeader}>
        <p className={styles.kicker}>Display</p>
        <h2>Theme settings</h2>
      </div>

      <div className={styles.toggleRow}>
        <div>
          <p>Dark mode</p>
          <small>Great for late study sessions.</small>
        </div>
        <label className={styles.switch}>
          <input
            type="checkbox"
            checked={isDarkMode}
            onChange={(e) => onToggle(e.target.checked)}
          />
          <span className={styles.slider}></span>
        </label>
      </div>

      <div className={styles.displayNote}>
        <span />
        <p>Changes apply immediately across the entire app.</p>
      </div>
    </section>
  );
}
