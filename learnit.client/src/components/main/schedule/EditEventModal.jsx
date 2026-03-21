import React from "react";
import styles from "../Schedule.module.css";
import Toggle from "../../ui/Toggle";

export function EditEventModal({
  isOpen,
  editingEvent,
  availableModules,
  editForm,
  onChange,
  onDelete,
  onSave,
  onClose,
  error,
}) {
  if (!isOpen || !editingEvent) return null;

  return (
    <div className={styles.modalOverlay}>
      <div className={styles.modal}>
        <div className={styles.modalHeader}>
          <div>
            <p className={styles.kicker}>Edit Event</p>
            <h2>Modify Schedule Item</h2>
            {editingEvent.courseModule && (
              <p className={styles.subtle}>
                Linked to {editingEvent.courseModule.courseTitle} ·{" "}
                {editingEvent.courseModule.title}
              </p>
            )}
          </div>
          <button className={styles.iconBtn} type="button" onClick={onClose}>
            ×
          </button>
        </div>

        <div className={styles.modalForm}>
          <label>
            Event Title *
            <input
              type="text"
              value={editForm.title}
              onChange={(e) => onChange({ title: e.target.value })}
              placeholder="Enter event title"
              required
            />
          </label>

          <div className={styles.formGrid}>
            <label>
              Start Time *
              <input
                type="datetime-local"
                value={editForm.start}
                onChange={(e) => onChange({ start: e.target.value })}
                required
              />
            </label>
            <label>
              End Time
              <input
                type="datetime-local"
                value={editForm.end}
                onChange={(e) => onChange({ end: e.target.value })}
              />
            </label>
          </div>

          {editingEvent.courseModule && (
            <div className={styles.moduleInfo}>
              <h4>Linked Course Module</h4>
              <div className={styles.moduleCard}>
                <strong>{editingEvent.courseModule.title}</strong>
                <small>from {editingEvent.courseModule.courseTitle}</small>
              </div>

              <div className={styles.toggleGrid}>
                <label className={styles.toggleRow}>
                  <span>Mark module complete</span>
                  <span className={styles.toggle}>
                    <input
                      type="checkbox"
                      checked={!!editForm.markComplete}
                      onChange={(e) =>
                        onChange({ markComplete: e.target.checked })
                      }
                      aria-label="Mark module complete"
                    />
                    <span className={styles.toggleSlider} />
                  </span>
                </label>

                <label className={styles.toggleRow}>
                  <span>Disconnect from course module</span>
                  <span className={styles.toggle}>
                    <input
                      type="checkbox"
                      checked={editForm.unlinkFromModule}
                      onChange={(e) =>
                        onChange({ unlinkFromModule: e.target.checked })
                      }
                      aria-label="Disconnect from course module"
                    />
                    <span className={styles.toggleSlider} />
                  </span>
                </label>
              </div>
            </div>
          )}

          {!editingEvent.courseModuleId && availableModules.length > 0 && (
            <label>
              Link to Course Module
              <select
                value={editForm.linkToModule}
                onChange={(e) => onChange({ linkToModule: e.target.value })}
              >
                <option value="">Choose a module (optional)</option>
                {availableModules.map((module) => (
                  <option key={module.id} value={module.id}>
                    {module.title} · {module.courseTitle} (
                    {module.estimatedHours}h)
                  </option>
                ))}
              </select>
            </label>
          )}

          {error && <div className={styles.errorMessage}>{error}</div>}

          <div className={styles.formActions}>
            <button
              className={styles.dangerBtn}
              type="button"
              onClick={onDelete}
            >
              Delete Event
            </button>
            <div className={styles.rightActions}>
              <button
                className={styles.secondaryBtn}
                type="button"
                onClick={onClose}
              >
                Cancel
              </button>
              <button
                className={styles.primaryBtn}
                type="button"
                onClick={onSave}
                disabled={!editForm.title.trim()}
              >
                Save Changes
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
