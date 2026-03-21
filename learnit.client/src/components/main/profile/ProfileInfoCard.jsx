import React from "react";
import styles from "../Profile.module.css";

export function ProfileInfoCard({ profile, saving, onChange, onSubmit }) {
  return (
    <section className={styles.card}>
      <div className={styles.cardHeader}>
        <p className={styles.kicker}>Basics</p>
        <h2>User information</h2>
      </div>

      <form onSubmit={onSubmit}>
        <label>
          Full Name
          <input
            type="text"
            value={profile.fullName}
            placeholder="Your name"
            onChange={(e) => onChange({ fullName: e.target.value })}
            required
          />
        </label>

        <label>
          Email Address
          <input
            type="email"
            value={profile.email}
            placeholder="Your email"
            onChange={(e) => onChange({ email: e.target.value })}
            required
          />
        </label>

        <div className={styles.cardActions}>
          <button type="submit" className={styles.primaryBtn} disabled={saving}>
            {saving ? "Updating..." : "Update Profile"}
          </button>
        </div>
      </form>
    </section>
  );
}
