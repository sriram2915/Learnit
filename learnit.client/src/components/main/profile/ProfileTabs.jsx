import React from "react";
import styles from "../Profile.module.css";

export function ProfileTabs({ activeSection, onChange }) {
  return (
    <div className={styles.tabs}>
      <button
        className={`${styles.tab} ${
          activeSection === "profile" ? styles.active : ""
        }`}
        onClick={() => onChange("profile")}
      >
        Profile Info
      </button>
      <button
        className={`${styles.tab} ${
          activeSection === "preferences" ? styles.active : ""
        }`}
        onClick={() => onChange("preferences")}
      >
        Preferences
      </button>
      <button
        className={`${styles.tab} ${
          activeSection === "security" ? styles.active : ""
        }`}
        onClick={() => onChange("security")}
      >
        Security
      </button>
      <button
        className={`${styles.tab} ${
          activeSection === "friends" ? styles.active : ""
        }`}
        onClick={() => onChange("friends")}
      >
        Friends
      </button>
    </div>
  );
}
