import React from "react";
import ReactMarkdown from "react-markdown";
import { FiUsers } from "react-icons/fi";
import styles from "../Ai.module.css";
export function ComparePanel({
  friends,
  selectedFriendIds,
  insights,
  loading,
  onSelectFriend,
  onCompare,
}) {
  return (
    <div className={styles.compareRow}>
      <div
        className={styles.card}
        style={{
          flex: 0.5,
          minWidth: 0,
          maxWidth: "320px",
          padding: "12px",
          gap: "8px",
        }}
      >
        <div className={styles.cardHeader} style={{ marginBottom: "4px" }}>
          <h3 style={{ fontSize: "1rem", margin: 0 }}>Select a friend</h3>
          <small style={{ fontSize: "0.82rem" }}>
            We’ll compare them with your progress
          </small>
        </div>
        <ul className={styles.friendList} style={{ gap: "6px" }}>
          {friends.map((f) => (
            <li key={f.id} style={{ padding: "7px 8px" }}>
              <label className={styles.friendRow} style={{ gap: "6px" }}>
                <input
                  type="radio"
                  name="friendCompare"
                  checked={selectedFriendIds.includes(f.id)}
                  onChange={() => onSelectFriend(f.id)}
                  disabled={loading}
                  style={{ marginRight: "6px" }}
                />
                <div>
                  <strong style={{ fontSize: "0.98rem" }}>
                    {f.displayName}
                  </strong>
                  <p
                    className={styles.muted}
                    style={{ fontSize: "0.85rem", margin: 0 }}
                  >
                    {f.completionRate}% · {f.weeklyHours}h/wk · {f.email}
                  </p>
                </div>
              </label>
            </li>
          ))}
          {!friends.length && (
            <li
              className={styles.muted}
              style={{ fontSize: "0.85rem", padding: "7px 8px" }}
            >
              No friends yet. Add them from Profile → Friends.
            </li>
          )}
        </ul>
        <button
          className={styles.primaryBtn}
          style={{
            padding: "8px 14px",
            fontSize: "0.98rem",
            borderRadius: "7px",
            marginTop: "4px",
          }}
          onClick={onCompare}
          disabled={!selectedFriendIds.length || loading}
        >
          <FiUsers /> <span style={{ marginLeft: 4 }}>Compare with AI</span>
        </button>
      </div>
      <div
        className={styles.card}
        style={{ flex: 1, minWidth: 0, padding: "12px", gap: "8px" }}
      >
        <div className={styles.cardHeader} style={{ marginBottom: "4px" }}>
          <h3 style={{ fontSize: "1rem", margin: 0 }}>Insights</h3>
          <small style={{ fontSize: "0.82rem" }}>User vs selected friend</small>
        </div>
        <div className={styles.insightBox}>
          {insights.length ? (
            insights.map((ins, i) => (
              <div key={i} className={styles.insightBlock}>
                <ReactMarkdown className={styles.markdown}>
                  {ins.detail}
                </ReactMarkdown>
              </div>
            ))
          ) : (
            <p className={styles.muted}>No insights yet.</p>
          )}
        </div>
      </div>
    </div>
  );
}
