import React from "react";
import styles from "../Schedule.module.css";

export function NextSessions({ events }) {
  const upcoming = events
    .filter((e) => new Date(e.start) > new Date())
    .sort((a, b) => new Date(a.start) - new Date(b.start))
    .slice(0, 3);

  return (
    <div className={styles.sideCard}>
      <div className={styles.cardHeader}>
        <h3>Next Sessions</h3>
        <span className={styles.nextBadge}>Your schedule</span>
      </div>
      <div className={styles.deepWorkList}>
        {upcoming.map((event) => {
          const startDate = new Date(event.start);
          const endDate = event.end ? new Date(event.end) : null;
          const duration = endDate
            ? Math.max(
                0.5,
                Math.round(((endDate - startDate) / (1000 * 60 * 60)) * 10) / 10
              )
            : 1;

          return (
            <div key={event.id} className={styles.sessionItem}>
              <div className={styles.sessionMeta}>
                <span>
                  {startDate.toLocaleDateString("en-US", {
                    weekday: "short",
                    hour: "numeric",
                    minute: "2-digit",
                  })}
                </span>
                <small>
                  {event.courseModuleId ? "Course module" : "Study session"}
                </small>
              </div>
              <strong>
                {event.title} · {duration}h
              </strong>
              <span className={styles.sessionType}>
                {event.courseModuleId ? "Linked module" : "Manual session"}
              </span>
            </div>
          );
        })}
        {upcoming.length === 0 && (
          <div className={styles.sessionItem}>
            <div className={styles.sessionMeta}>
              <span>No upcoming sessions</span>
              <small>Create or schedule some sessions</small>
            </div>
            <strong>Get started with your study plan</strong>
            <span className={styles.sessionType}>Plan ahead</span>
          </div>
        )}
      </div>
    </div>
  );
}
