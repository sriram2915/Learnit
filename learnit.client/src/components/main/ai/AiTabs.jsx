import React from "react";
import styles from "../Ai.module.css";

export function AiTabs({ activeTab, onChange }) {
  return (
    <div className={styles.tabs} style={{ gap: "6px", margin: "2px 0 8px" }}>
      <button
        className={`${styles.tab} ${activeTab === "chat" ? styles.active : ""}`}
        style={{ padding: "7px 12px", fontSize: "0.97rem", borderRadius: 0 }}
        onClick={() => onChange("chat")}
      >
        Chat
      </button>
      <button
        className={`${styles.tab} ${
          activeTab === "compare" ? styles.active : ""
        }`}
        style={{ padding: "7px 12px", fontSize: "0.97rem", borderRadius: 0 }}
        onClick={() => onChange("compare")}
      >
        Insights & Friends
      </button>
    </div>
  );
}
