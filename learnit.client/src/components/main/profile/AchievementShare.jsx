import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { progressApi, profileApi } from "../../../services";
import styles from "./ShareableCard.module.css";

function AchievementShare() {
  const { userId } = useParams();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [stats, setStats] = useState({
    currentStreak: 0,
    longestStreak: 0,
    totalCompletedHours: 0,
    coursesCompleted: 0,
    totalCourses: 0,
  });
  const [profile, setProfile] = useState({
    fullName: "",
    email: "",
  });
  const [heatmapData, setHeatmapData] = useState([]);

  // Helper function to update meta tags
  const updateMetaTags = (userName, description, shareUrl) => {
    const updateMetaTag = (property, content, isProperty = false) => {
      let selector = isProperty 
        ? `meta[property="${property}"]` 
        : `meta[name="${property}"]`;
      let element = document.querySelector(selector);
      
      if (!element) {
        element = document.createElement("meta");
        if (isProperty) {
          element.setAttribute("property", property);
        } else {
          element.setAttribute("name", property);
        }
        document.head.appendChild(element);
      }
      element.setAttribute("content", content);
    };

    // Open Graph tags (Facebook, LinkedIn, etc.)
    updateMetaTag("og:title", `${userName}'s Learning Progress on LearnIt`, true);
    updateMetaTag("og:description", description, true);
    updateMetaTag("og:url", shareUrl, true);
    updateMetaTag("og:type", "website", true);
    updateMetaTag("og:image", `${shareUrl}?preview=true`, true);
    updateMetaTag("og:site_name", "LearnIt", true);
    
    // Twitter Card tags (use name attribute)
    updateMetaTag("twitter:card", "summary_large_image", false);
    updateMetaTag("twitter:title", `${userName}'s Learning Progress`, false);
    updateMetaTag("twitter:description", description, false);
    updateMetaTag("twitter:image", `${shareUrl}?preview=true`, false);
    
    // Standard meta tags
    document.title = `${userName}'s Learning Progress | LearnIt`;
    updateMetaTag("description", description, false);
  };

  // Update meta tags when data loads
  useEffect(() => {
    if (!loading && profile.fullName && stats.currentStreak >= 0) {
      const userName = profile.fullName || profile.email?.split("@")[0] || "Learner";
      const shareUrl = window.location.href;
      const description = `🔥 ${stats.currentStreak} day streak | 📚 ${stats.coursesCompleted} courses completed | ⏱️ ${Math.round(stats.totalCompletedHours)} hours studied on LearnIt!`;
      updateMetaTags(userName, description, shareUrl);
    }

    return () => {
      document.title = "LearnIt";
    };
  }, [loading, profile, stats]);

  useEffect(() => {
    if (userId) {
      loadData();
    }
  }, [userId]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError("");

      // For now, load current user's data
      // TODO: Create public API endpoint to fetch any user's achievement data
      const token = localStorage.getItem("token");
      if (!token) {
        setError("Please log in to view achievements");
        setLoading(false);
        return;
      }

      const profileData = await profileApi.getProfile();
      setProfile(profileData.profile || profileData);

      // Load progress data
      const progressData = await progressApi.getProgressDashboard();

      const progressStats = {
        currentStreak: Number(
          progressData.stats?.currentStreak ??
            progressData.Stats?.CurrentStreak ??
            0
        ),
        longestStreak: Number(
          progressData.stats?.longestStreak ??
            progressData.Stats?.LongestStreak ??
            0
        ),
        totalCompletedHours: Number(
          progressData.stats?.totalCompletedHours ??
            progressData.Stats?.TotalCompletedHours ??
            0
        ),
      };

      const courseProgress =
        progressData.courseProgress || progressData.CourseProgress || [];
      const completedCourses = courseProgress.filter(
        (c) => c.progressPercentage === 100 || c.progressPercentage >= 100
      ).length;
      const totalCourses = courseProgress.length;

      setStats({
        ...progressStats,
        coursesCompleted: completedCourses,
        totalCourses: totalCourses,
      });

      const heatmap =
        progressData.activityHeatmap ||
        progressData.ActivityHeatmap ||
        Array(60).fill(0);
      setHeatmapData(heatmap);
    } catch (err) {
      setError("Failed to load achievement data");
      console.error("AchievementShare error:", err);
    } finally {
      setLoading(false);
    }
  };

  const generateHeatmap = () => {
    const weeks = 7;
    const daysPerWeek = 7;
    const totalDays = weeks * daysPerWeek;
    const recentData = heatmapData.slice(-totalDays);

    const weeksData = [];
    for (let w = 0; w < weeks; w++) {
      const weekData = [];
      for (let d = 0; d < daysPerWeek; d++) {
        const idx = w * daysPerWeek + d;
        weekData.push(recentData[idx] || 0);
      }
      weeksData.push(weekData);
    }
    return weeksData;
  };

  const getHeatmapColor = (intensity) => {
    if (intensity === 0) return "#ebedf0";
    if (intensity === 1) return "#c6e48b";
    if (intensity === 2) return "#7bc96f";
    if (intensity === 3) return "#239a3b";
    return "#196127";
  };

  if (loading) {
    return (
      <div className={styles.publicContainer}>
        <div className={styles.loading}>Loading achievement...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.publicContainer}>
        <div className={styles.error}>{error}</div>
      </div>
    );
  }

  const heatmapWeeks = generateHeatmap();
  const userName = profile.fullName || profile.email?.split("@")[0] || "Learner";
  const userInitials = userName
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);

  return (
    <div className={styles.publicContainer}>
      <div className={styles.shareableCard}>
        <div className={styles.header}>
          <div className={styles.logo}>
            <div className={styles.logoIcon}>📚</div>
            <span className={styles.logoText}>LearnIt</span>
          </div>
          <div className={styles.userInfo}>
            <div className={styles.avatar}>{userInitials}</div>
            <div className={styles.userDetails}>
              <div className={styles.userName}>{userName}</div>
              <div className={styles.userEmail}>
                {profile.email || "learner@learnit.com"}
              </div>
            </div>
          </div>
        </div>

        <div className={styles.statsGrid}>
          <div className={styles.statCard}>
            <div className={styles.statIcon}>🔥</div>
            <div className={styles.statValue}>{stats.currentStreak}</div>
            <div className={styles.statLabel}>Day Streak</div>
          </div>

          <div className={styles.statCard}>
            <div className={styles.statIcon}>🏆</div>
            <div className={styles.statValue}>{stats.longestStreak}</div>
            <div className={styles.statLabel}>Best Streak</div>
          </div>

          <div className={styles.statCard}>
            <div className={styles.statIcon}>📚</div>
            <div className={styles.statValue}>
              {stats.coursesCompleted}/{stats.totalCourses}
            </div>
            <div className={styles.statLabel}>Courses</div>
          </div>

          <div className={styles.statCard}>
            <div className={styles.statIcon}>⏱️</div>
            <div className={styles.statValue}>
              {Math.round(stats.totalCompletedHours)}
            </div>
            <div className={styles.statLabel}>Hours Studied</div>
          </div>
        </div>

        <div className={styles.heatmapSection}>
          <div className={styles.heatmapTitle}>Activity Heatmap</div>
          <div className={styles.heatmap}>
            {heatmapWeeks.map((week, weekIdx) => (
              <div key={weekIdx} className={styles.heatmapWeek}>
                {week.map((day, dayIdx) => (
                  <div
                    key={dayIdx}
                    className={styles.heatmapDay}
                    style={{ backgroundColor: getHeatmapColor(day) }}
                  />
                ))}
              </div>
            ))}
          </div>
          <div className={styles.heatmapLegend}>
            <span>Less</span>
            <div className={styles.legendColors}>
              <div
                className={styles.legendColor}
                style={{ backgroundColor: "#ebedf0" }}
              />
              <div
                className={styles.legendColor}
                style={{ backgroundColor: "#c6e48b" }}
              />
              <div
                className={styles.legendColor}
                style={{ backgroundColor: "#7bc96f" }}
              />
              <div
                className={styles.legendColor}
                style={{ backgroundColor: "#239a3b" }}
              />
              <div
                className={styles.legendColor}
                style={{ backgroundColor: "#196127" }}
              />
            </div>
            <span>More</span>
          </div>
        </div>

        <div className={styles.footer}>
          <div className={styles.footerText}>
            Keep learning, keep growing! 🌱
          </div>
          <div className={styles.footerBrand}>learnit.app</div>
        </div>
      </div>
    </div>
  );
}

export default AchievementShare;

