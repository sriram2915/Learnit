import React from "react";
import styles from "../Profile.module.css";

export function FriendsCard({
  friends,
  friendsLoading,
  friendEmail,
  saving,
  onEmailChange,
  onAdd,
  onRemove,
}) {
  return (
    <section className={styles.card}>
      <div className={styles.cardHeader}>
        <p className={styles.kicker}>Friends</p>
        <h2>Connect with Learnit users</h2>
        <small>Add by email, no confirmation required.</small>
      </div>

      <form className={styles.inlineForm} onSubmit={onAdd}>
        <input
          type="email"
          placeholder="friend@email.com"
          value={friendEmail}
          onChange={(e) => onEmailChange(e.target.value)}
          required
        />
        <button type="submit" className={styles.primaryBtn} disabled={saving}>
          {saving ? "Adding..." : "Add Friend"}
        </button>
      </form>

      <div className={styles.listHeader}>
        <p>Friends ({friends.length})</p>
        {friendsLoading && <small>Refreshing…</small>}
      </div>
      <ul className={styles.friendList}>
        {friends.map((f) => (
          <li key={f.id} className={styles.friendItem}>
            <div>
              <strong>{f.displayName}</strong>
              <p className={styles.muted}>{f.email}</p>
              <p className={styles.muted}>
                {f.completionRate}% complete · {f.weeklyHours}h/wk
              </p>
            </div>
            <button
              type="button"
              className={styles.linkBtn}
              onClick={() => onRemove(f.id)}
            >
              Remove
            </button>
          </li>
        ))}
        {!friends.length && !friendsLoading && (
          <li className={styles.muted}>No friends yet.</li>
        )}
      </ul>
    </section>
  );
}
