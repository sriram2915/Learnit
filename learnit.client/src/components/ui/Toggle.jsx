import React from "react";
import styles from "./ui.module.css";

export default function Toggle({ checked, onChange, label, ...props }) {
  return (
    <label className={styles.toggleRoot}>
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className={styles.toggleInput}
        {...props}
      />
      <span className={styles.toggleSlider} />
      {label && <span className={styles.toggleLabel}>{label}</span>}
    </label>
  );
}
