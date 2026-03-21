import React from "react";
import styles from "../Schedule.module.css";

export function RescheduleModal({
  isOpen,
  onClose,
  missedEvents = [],
  onDismiss,
}) {
  if (!isOpen || !missedEvents || missedEvents.length === 0) return null;

  const formatEventTime = (start, end) => {
    const startDate = new Date(start);
    const endDate = end ? new Date(end) : new Date(startDate.getTime() + 60 * 60 * 1000);
    
    const dateStr = startDate.toLocaleDateString(undefined, { 
      weekday: 'short', 
      month: 'short', 
      day: 'numeric' 
    });
    const startTime = startDate.toLocaleTimeString(undefined, { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
    const endTime = endDate.toLocaleTimeString(undefined, { 
      hour: '2-digit', 
      minute: '2-digit' 
    });
    
    return `${dateStr}, ${startTime} - ${endTime}`;
  };

  return (
    <div className={styles.modalOverlay}>
      <div className={styles.modal} style={{ maxWidth: '600px' }}>
        <div className={styles.modalHeader}>
          <div>
            <p className={styles.kicker}>Reminder</p>
            <h2>Missed Incomplete Sessions</h2>
            <p className={styles.subtle}>
              You have {missedEvents.length} incomplete session{missedEvents.length !== 1 ? 's' : ''} that {missedEvents.length === 1 ? 'has' : 'have'} passed.
              Please manually reschedule {missedEvents.length === 1 ? 'it' : 'them'} using the scheduling interface.
            </p>
          </div>
          <button className={styles.iconBtn} type="button" onClick={onClose}>
            ×
          </button>
        </div>

        <div className={styles.modalForm}>
          <div className={styles.missedEventsList}>
            {missedEvents.map((event) => (
              <div key={event.id} className={styles.missedEventItem}>
                <div className={styles.missedEventInfo}>
                  <h4>{event.title}</h4>
                  <p className={styles.missedEventTime}>
                    {formatEventTime(event.start, event.end)}
                  </p>
                  {event.courseModule && (
                    <p className={styles.missedEventCourse}>
                      {event.courseModule.courseTitle} → {event.courseModule.title}
                    </p>
                  )}
                </div>
              </div>
            ))}
          </div>

          <div className={styles.modalActions}>
            <button
              className={styles.primaryBtn}
              onClick={onDismiss}
            >
              Got it
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

