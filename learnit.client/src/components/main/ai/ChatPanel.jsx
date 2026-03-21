import React from "react";
import ReactMarkdown from "react-markdown";
import { FiSend, FiZap, FiBarChart2 } from "react-icons/fi";
import styles from "../Ai.module.css";

export function ChatPanel({
  messages,
  loading,
  input,
  onInputChange,
  onSend,
  onQuickSend,
}) {
  // Allow Shift+Enter for newline, Enter for send
  const handleKeyDown = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (!loading && input.trim()) onSend();
    }
  };
  // Fixed chat height (e.g. 420px), fixed messages area, input always at bottom
  const CHAT_HEIGHT = 420;
  const INPUT_ROW_HEIGHT = 56; // px, approx for textarea+button
  return (
    <div
      className={styles.chatPanel}
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "stretch",
        height: CHAT_HEIGHT,
        minHeight: CHAT_HEIGHT,
        maxHeight: CHAT_HEIGHT,
        width: "100%",
        position: "relative",
        boxSizing: "border-box",
      }}
    >
      <div
        className={styles.chatCard}
        style={{
          height: "100%",
          minHeight: 0,
          display: "flex",
          flexDirection: "column",
          padding: 0,
          position: "relative",
        }}
      >
        <div
          className={styles.messages}
          style={{
            flex: 1,
            minHeight: 0,
            maxHeight: CHAT_HEIGHT - INPUT_ROW_HEIGHT,
            overflowY: "auto",
            padding: "10px",
            gap: "7px",
            marginBottom: 0,
            background: "var(--bg)",
          }}
        >
          {messages.map((m, idx) => (
            <div
              key={idx}
              className={`${styles.message} ${
                m.role === "assistant" ? styles.assistant : styles.user
              }`}
              style={{
                padding: "10px 12px",
                fontSize: "0.97rem",
                borderRadius: "8px",
              }}
            >
              <ReactMarkdown className={styles.markdown}>
                {m.content}
              </ReactMarkdown>
            </div>
          ))}
          {loading && (
            <div
              className={`${styles.message} ${styles.assistant}`}
              style={{
                padding: "10px 12px",
                fontSize: "0.97rem",
                borderRadius: "8px",
              }}
            >
              <div className={styles.typingIndicator}>
                <span></span>
                <span></span>
                <span></span>
              </div>
              <span style={{ marginLeft: "8px" }}>Thinking...</span>
            </div>
          )}
        </div>
        <div
          className={styles.inputRow}
          style={{
            gap: "7px",
            padding: "10px",
            position: "absolute",
            left: 0,
            right: 0,
            bottom: 0,
            background: "var(--surface)",
            zIndex: 2,
            borderTop: "1px solid var(--border)",
            height: INPUT_ROW_HEIGHT,
            boxSizing: "border-box",
          }}
        >
          <div className={styles.quickActions} style={{ gap: "6px" }}>
            <button
              type="button"
              className={styles.pill}
              style={{
                padding: "7px 12px",
                fontSize: "0.93rem",
                borderRadius: "7px",
              }}
              onClick={() =>
                onQuickSend("Give me 3 schedule tweaks for this week")
              }
              disabled={loading}
            >
              <FiZap /> <span style={{ marginLeft: 3 }}>Schedule tips</span>
            </button>
            <button
              type="button"
              className={styles.pill}
              style={{
                padding: "7px 12px",
                fontSize: "0.93rem",
                borderRadius: "7px",
              }}
              onClick={() =>
                onQuickSend(
                  "Give me 3 quick progress insights and next actions"
                )
              }
              disabled={loading}
            >
              <FiBarChart2 /> Progress tips
            </button>
          </div>
          <div className={styles.inputFlex} style={{ alignItems: "flex-end" }}>
            <textarea
              value={input}
              onChange={(e) => onInputChange(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask anything about your study plan..."
              rows={1}
              style={{
                resize: "vertical",
                minHeight: 36,
                maxHeight: 120,
                flex: 1,
                fontSize: "0.93rem",
                borderRadius: 7,
                padding: "7px 10px",
                border: "1px solid var(--border)",
                background: "var(--surface)",
                color: "var(--text-strong)",
                lineHeight: 1.4,
              }}
              disabled={loading}
            />
            <button
              onClick={onSend}
              disabled={loading || !input.trim()}
              style={{
                marginLeft: 6,
                padding: "7px 12px",
                borderRadius: 7,
                fontSize: "0.93rem",
              }}
            >
              <FiSend /> Send
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
