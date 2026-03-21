import { useState, useEffect } from "react";
import { profileApi, aiApi } from "../../services";
import { useTheme } from "../../context/useTheme";
import { ProfileInfoCard } from "./profile/ProfileInfoCard";
import { ThemeCard } from "./profile/ThemeCard";
import { PasswordCard } from "./profile/PasswordCard";
import { FriendsCard } from "./profile/FriendsCard";
import styles from "./Profile.module.css";

function Profile() {
  const { isDarkMode, setDarkMode } = useTheme();

  const [profile, setProfile] = useState({
    fullName: "",
    email: "",
  });

  const [preferences, setPreferences] = useState({
    studySpeed: "normal",
    maxSessionMinutes: 60,
    weeklyStudyLimitHours: 10,
    darkMode: false,
  });

  const [passwordData, setPasswordData] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  const [friendEmail, setFriendEmail] = useState("");
  const [friends, setFriends] = useState([]);
  const [friendsLoading, setFriendsLoading] = useState(false);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [preferencesLoaded, setPreferencesLoaded] = useState(false);

  useEffect(() => {
    loadProfile();
    loadFriends();
  }, []);

  // Sync theme context when preferences are loaded from server (not on initial default state)
  useEffect(() => {
    // Only sync theme after preferences have been loaded from the server
    // This prevents the flash from dark -> light -> dark when navigating to profile
    if (preferencesLoaded) {
      setDarkMode(preferences.darkMode);
    }
  }, [preferences.darkMode, preferencesLoaded, setDarkMode]);

  const loadProfile = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await profileApi.getProfile();
      setProfile(data.profile || data);
      const loadedPreferences = data.preferences || {
        studySpeed: "normal",
        maxSessionMinutes: 60,
        weeklyStudyLimitHours: 10,
        darkMode: false,
      };
      setPreferences(loadedPreferences);
      // Mark preferences as loaded so theme sync can happen
      setPreferencesLoaded(true);
    } catch (err) {
      setError("Failed to load profile");
      console.error("Profile loading error:", err);
      // Still mark as loaded even on error to prevent infinite waiting
      setPreferencesLoaded(true);
    } finally {
      setLoading(false);
    }
  };

  const handleProfileUpdate = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      setError("");
      setSuccess("");

      await profileApi.updateProfile(profile);
      setSuccess("Profile updated successfully!");
      setTimeout(() => setSuccess(""), 3000);
    } catch (err) {
      setError(err.message || "Failed to update profile");
      setTimeout(() => setError(""), 3000);
    } finally {
      setSaving(false);
    }
  };

  const handlePasswordChange = async (e) => {
    e.preventDefault();
    setError("");
    setSuccess("");

    // Validation
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setError("New passwords do not match");
      return;
    }

    if (passwordData.newPassword.length < 6) {
      setError("Password must be at least 6 characters long");
      return;
    }

    if (!passwordData.currentPassword) {
      setError("Current password is required");
      return;
    }

    try {
      setSaving(true);
      await profileApi.changePassword({
        oldPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword,
      });
      setSuccess("Password changed successfully!");
      setPasswordData({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      });
      setTimeout(() => setSuccess(""), 3000);
    } catch (err) {
      setError(err.message || "Failed to change password");
      setTimeout(() => setError(""), 3000);
    } finally {
      setSaving(false);
    }
  };

  const loadFriends = async () => {
    try {
      setFriendsLoading(true);
      const data = await aiApi.listFriends();
      setFriends(data);
    } catch (err) {
      setError("Failed to load friends");
      setTimeout(() => setError(""), 2500);
    } finally {
      setFriendsLoading(false);
    }
  };

  const handleAddFriend = async (e) => {
    e.preventDefault();
    if (!friendEmail.trim()) return;
    try {
      setSaving(true);
      const added = await aiApi.addFriend({ email: friendEmail.trim() });
      setFriends((prev) => [...prev, added]);
      setFriendEmail("");
      setSuccess("Friend added");
      setTimeout(() => setSuccess(""), 2000);
    } catch (err) {
      setError(err.message || "Failed to add friend");
      setTimeout(() => setError(""), 2500);
    } finally {
      setSaving(false);
    }
  };

  const handleRemoveFriend = async (id) => {
    try {
      await aiApi.deleteFriend(id);
      setFriends((prev) => prev.filter((f) => f.id !== id));
    } catch (err) {
      setError("Failed to remove friend");
      setTimeout(() => setError(""), 2500);
    }
  };

  const handleThemeToggle = async (enabled) => {
    setDarkMode(enabled);
    setPreferences((prev) => ({ ...prev, darkMode: enabled }));

    try {
      await profileApi.updatePreferences({
        ...preferences,
        darkMode: enabled,
      });
    } catch (err) {
      setDarkMode(!enabled);
      setPreferences((prev) => ({ ...prev, darkMode: !enabled }));
      setError("Failed to update theme preference");
      setTimeout(() => setError(""), 3000);
    }
  };

  if (loading) {
    return (
      <section className={styles.profile}>
        <div
          style={{
            textAlign: "center",
            padding: "40px",
            color: "var(--muted)",
          }}
        >
          Loading profile...
        </div>
      </section>
    );
  }

  return (
    <section className={styles.profile}>
      {error && <div className={styles.errorMessage}>{error}</div>}

      {success && <div className={styles.successMessage}>{success}</div>}

      <div className={styles.grid}>
        <ProfileInfoCard
          profile={profile}
          saving={saving}
          onChange={(updates) =>
            setProfile((prev) => ({ ...prev, ...updates }))
          }
          onSubmit={handleProfileUpdate}
        />

        <PasswordCard
          passwordData={passwordData}
          saving={saving}
          onChange={(updates) =>
            setPasswordData((prev) => ({ ...prev, ...updates }))
          }
          onSubmit={handlePasswordChange}
        />

        <ThemeCard isDarkMode={isDarkMode} onToggle={handleThemeToggle} />

        <FriendsCard
          friends={friends}
          friendsLoading={friendsLoading}
          friendEmail={friendEmail}
          saving={saving}
          onEmailChange={setFriendEmail}
          onAdd={handleAddFriend}
          onRemove={handleRemoveFriend}
        />
      </div>
    </section>
  );
}

export default Profile;
