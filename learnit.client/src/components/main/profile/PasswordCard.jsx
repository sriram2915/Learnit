import React from "react";
import styles from "../Profile.module.css";

export function PasswordCard({ passwordData, saving, onChange, onSubmit }) {
  return (
    <section className={styles.card}>
      <div className={styles.cardHeader}>
        <p className={styles.kicker}>Security</p>
        <h2>Change password</h2>
      </div>

      <form onSubmit={onSubmit}>
        <label>
          Current Password
          <input
            type="password"
            value={passwordData.currentPassword}
            placeholder="••••••••"
            onChange={(e) => onChange({ currentPassword: e.target.value })}
            required
          />
        </label>

        <label>
          New Password
          <input
            type="password"
            value={passwordData.newPassword}
            placeholder="••••••••"
            onChange={(e) => onChange({ newPassword: e.target.value })}
            required
            minLength="6"
          />
        </label>

        <label>
          Confirm New Password
          <input
            type="password"
            value={passwordData.confirmPassword}
            placeholder="••••••••"
            onChange={(e) => onChange({ confirmPassword: e.target.value })}
            required
            minLength="6"
          />
        </label>

        <div className={styles.cardActions}>
          <button type="submit" className={styles.dangerBtn} disabled={saving}>
            {saving ? "Changing..." : "Change Password"}
          </button>
        </div>
      </form>
    </section>
  );
}
