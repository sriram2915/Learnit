import React from "react";
import Modal from "../../ui/Modal";
import ui from "../../ui/ui.module.css";
import Toggle from "../../ui/Toggle";

export function AutoScheduleModal({
  isOpen,
  autoOptions,
  onChange,
  courses = [],
  onToggleCourse,
  onMoveCourse,
  onClose,
  onSubmit,
  loading,
  error,
}) {
  if (!isOpen) return null;

  return (
    <Modal
      title="Quick schedule"
      kicker="Auto-plan"
      onClose={onClose}
      actions={
        <>
          <button className={ui.button} type="button" onClick={onClose}>
            Cancel
          </button>
          <button
            className={ui.buttonPrimary}
            type="button"
            onClick={onSubmit}
            disabled={loading}
          >
            Run auto-schedule
          </button>
        </>
      }
    >
      <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(3, 1fr)",
            gap: 12,
          }}
        >
          <label className={ui.field}>
            <span>Start date</span>
            <input
              type="date"
              value={autoOptions.startDate}
              onChange={(e) => onChange({ startDate: e.target.value })}
            />
          </label>
          <label className={ui.field}>
            <span>Day start</span>
            <input
              type="time"
              value={autoOptions.dayStart}
              onChange={(e) => onChange({ dayStart: e.target.value })}
            />
          </label>
          <label className={ui.field}>
            <span>Day end</span>
            <input
              type="time"
              value={autoOptions.dayEnd}
              onChange={(e) => onChange({ dayEnd: e.target.value })}
            />
          </label>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <Toggle
            checked={!!autoOptions.includeWeekends}
            onChange={(e) => onChange({ includeWeekends: e.target.checked })}
            label="Allow weekends"
            name="includeWeekends"
          />
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(3, 1fr)",
            gap: 12,
          }}
        >
          <label className={ui.field}>
            <span>Max session minutes</span>
            <input
              type="number"
              min="30"
              max="180"
              step="15"
              value={autoOptions.maxSessionMinutes}
              onChange={(e) =>
                onChange({
                  maxSessionMinutes:
                    Number(e.target.value) || autoOptions.maxSessionMinutes,
                })
              }
            />
          </label>
          <label className={ui.field}>
            <span>Buffer (min)</span>
            <input
              type="number"
              min="5"
              max="45"
              value={autoOptions.bufferMinutes}
              onChange={(e) =>
                onChange({
                  bufferMinutes:
                    Number(e.target.value) || autoOptions.bufferMinutes,
                })
              }
            />
          </label>
          <label className={ui.field}>
            <span>Weekly cap (hours)</span>
            <input
              type="number"
              min="0"
              max="60"
              value={autoOptions.weeklyLimitHours}
              onChange={(e) =>
                onChange({ weeklyLimitHours: Number(e.target.value) })
              }
            />
          </label>
        </div>

        <div className={ui.card} style={{ marginTop: 8 }}>
          <div style={{ marginBottom: 8 }}>
            <span style={{ fontWeight: 600 }}>Courses to schedule</span>
            <small style={{ marginLeft: 8, color: "var(--muted)" }}>
              Select and order priority
            </small>
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {courses.length === 0 && (
              <p style={{ color: "var(--muted)" }}>No courses found</p>
            )}
            {courses.map((course) => {
              const idx = (autoOptions.courseOrder || []).indexOf(course.id);
              const selected = idx !== -1;
              return (
                <div
                  key={course.id}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    background: selected ? "var(--surface)" : undefined,
                    border: selected
                      ? "1.5px solid var(--accent)"
                      : "1.5px solid var(--border)",
                    borderRadius: 6,
                    padding: "6px 8px",
                  }}
                >
                  <label
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: 8,
                      flex: 1,
                    }}
                  >
                    <input
                      type="checkbox"
                      checked={selected}
                      onChange={() => onToggleCourse?.(course.id)}
                    />
                    <div style={{ flex: 1 }}>
                      <span style={{ fontWeight: 500 }}>{course.title}</span>
                      {course.priority && (
                        <small style={{ color: "var(--muted)", marginLeft: 6 }}>
                          {course.priority} priority
                        </small>
                      )}
                    </div>
                  </label>
                  {selected && (
                    <div
                      style={{ display: "flex", alignItems: "center", gap: 4 }}
                    >
                      <span
                        style={{
                          fontSize: 13,
                          color: "var(--accent)",
                          fontWeight: 600,
                        }}
                      >
                        #{idx + 1}
                      </span>
                      <div
                        style={{
                          display: "flex",
                          flexDirection: "column",
                          gap: 2,
                        }}
                      >
                        <button
                          type="button"
                          onClick={() => onMoveCourse?.(course.id, -1)}
                          disabled={idx === 0}
                          aria-label="Move up"
                          style={{
                            padding: 0,
                            border: "none",
                            background: "none",
                            cursor: "pointer",
                          }}
                        >
                          ↑
                        </button>
                        <button
                          type="button"
                          onClick={() => onMoveCourse?.(course.id, 1)}
                          disabled={
                            idx === (autoOptions.courseOrder?.length || 0) - 1
                          }
                          aria-label="Move down"
                          style={{
                            padding: 0,
                            border: "none",
                            background: "none",
                            cursor: "pointer",
                          }}
                        >
                          ↓
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {error && <div className={ui.errorBanner}>{error}</div>}
      </div>
    </Modal>
  );
}
