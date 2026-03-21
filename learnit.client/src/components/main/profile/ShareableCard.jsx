import { useState, useRef, useEffect, useContext } from "react";
import html2canvas from "html2canvas";
import { AuthContext } from "../../../context/AuthContext";
import { progressApi, profileApi } from "../../../services";
import styles from "./ShareableCard.module.css";
import { Button } from "../../ui/index";

function ShareableCard() {
  const { user } = useContext(AuthContext);
  const [loading, setLoading] = useState(true);
  const [sharing, setSharing] = useState(false);
  const [error, setError] = useState("");
  const [shareUrl, setShareUrl] = useState("");
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
  const cardRef = useRef(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError("");

      // Load profile
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

      // Count completed courses
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

      // Load heatmap data (last 60 days)
      const heatmap =
        progressData.activityHeatmap ||
        progressData.ActivityHeatmap ||
        Array(60).fill(0);
      setHeatmapData(heatmap);

      // Generate shareable URL
      if (user?.id) {
        const userId = user.id;
        const baseUrl = window.location.origin;
        setShareUrl(`${baseUrl}/share/${userId}`);
      }
    } catch (err) {
      setError("Failed to load data");
      console.error("ShareableCard error:", err);
    } finally {
      setLoading(false);
    }
  };

  const generateHeatmap = () => {
    const weeks = 7; // 7 weeks
    const daysPerWeek = 7;
    const totalDays = weeks * daysPerWeek; // 49 days (last 7 weeks)
    const recentData = heatmapData.slice(-totalDays);

    // Group into weeks
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

  const downloadImage = async () => {
    if (!cardRef.current) return;

    try {
      setSharing(true);
      const canvas = await html2canvas(cardRef.current, {
        backgroundColor: "#0a0e27",
        scale: 2,
        logging: false,
        useCORS: true,
      });

      const link = document.createElement("a");
      link.download = `learnit-achievement-${Date.now()}.png`;
      link.href = canvas.toDataURL("image/png");
      link.click();
    } catch (err) {
      setError("Failed to generate image");
      console.error("Image generation error:", err);
    } finally {
      setSharing(false);
    }
  };

  const shareToSocial = async () => {
    if (!shareUrl) {
      setError("Share URL not available");
      return;
    }

    const userName = profile.fullName || profile.email?.split("@")[0] || "Learner";
    const shareText = `🔥 Check out my learning progress on LearnIt! 

📚 ${stats.coursesCompleted} courses completed
🔥 ${stats.currentStreak} day streak  
⏱️ ${Math.round(stats.totalCompletedHours)} hours studied

View my full stats: ${shareUrl}`;

    try {
      setSharing(true);

      // Generate preview image for better social sharing
      let imageBlob = null;
      if (cardRef.current) {
        try {
          const canvas = await html2canvas(cardRef.current, {
            backgroundColor: "#0a0e27",
            scale: 2,
            logging: false,
            useCORS: true,
          });
          imageBlob = await new Promise((resolve) =>
            canvas.toBlob(resolve, "image/png")
          );
        } catch (imgErr) {
          console.warn("Failed to generate preview image:", imgErr);
        }
      }

      // Use Web Share API if available (for mobile - supports files)
      if (navigator.share) {
        try {
          const shareData = {
            title: `${userName}'s Learning Progress on LearnIt!`,
            text: shareText,
            url: shareUrl,
          };

          // Add image if available (some platforms support this)
          if (imageBlob && navigator.canShare) {
            const file = new File([imageBlob], "learnit-achievement.png", {
              type: "image/png",
            });
            if (navigator.canShare({ files: [file] })) {
              shareData.files = [file];
            }
          }

          await navigator.share(shareData);
          return;
        } catch (shareErr) {
          if (shareErr.name === "AbortError") {
            return; // User cancelled
          }
        }
      }

      // Fallback: Copy link to clipboard
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(shareUrl);
        setError("");
        alert("✅ Link copied! Paste it anywhere - Instagram, Facebook, LinkedIn, WhatsApp, Twitter, etc. The preview card will show automatically! 🎉");
      } else {
        // Fallback: Select text for manual copy
        const textArea = document.createElement("textarea");
        textArea.value = shareUrl;
        textArea.style.position = "fixed";
        textArea.style.opacity = "0";
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand("copy");
        document.body.removeChild(textArea);
        alert("✅ Link copied! Paste it anywhere - the preview card will show automatically! 🎉");
      }
    } catch (err) {
      if (err.name !== "AbortError") {
        setError("Failed to share");
        console.error("Share error:", err);
      }
    } finally {
      setSharing(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.card}>
        <div className={styles.loading}>Loading achievement card...</div>
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
    <div className={styles.container}>
      {error && <div className={styles.error}>{error}</div>}

      <div ref={cardRef} className={styles.shareableCard}>
        {/* Header with gradient */}
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

        {/* Main stats grid */}
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

        {/* Heatmap */}
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
                    title={`Day ${weekIdx * 7 + dayIdx + 1}: Level ${day}`}
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

        {/* Footer */}
        <div className={styles.footer}>
          <div className={styles.footerText}>
            Keep learning, keep growing! 🌱
          </div>
          <div className={styles.footerBrand}>learnit.app</div>
        </div>
      </div>

      {/* Action buttons */}
      <div className={styles.actions}>
        <Button
          variant="primary"
          onClick={shareToSocial}
          disabled={sharing || !shareUrl}
          className={styles.shareButton}
        >
          {sharing ? "Copying..." : "📤 Copy Share Link"}
        </Button>
        <Button
          variant="ghost"
          onClick={downloadImage}
          disabled={sharing}
          className={styles.downloadButton}
        >
          {sharing ? "Generating..." : "💾 Download Image"}
        </Button>
        {shareUrl && (
          <div className={styles.shareUrlContainer}>
            <div className={styles.shareHint}>
              💡 <strong>Share like Strava!</strong> Copy this link and paste it anywhere - Instagram, Facebook, LinkedIn, WhatsApp, Twitter, etc. The preview card will show automatically!
            </div>
            <input
              type="text"
              value={shareUrl}
              readOnly
              className={styles.shareUrlInput}
              onClick={(e) => e.target.select()}
            />
            <Button
              variant="ghost"
              size="small"
              onClick={() => {
                navigator.clipboard.writeText(shareUrl);
                alert("✅ Link copied! Paste it anywhere!");
              }}
              className={styles.copyButton}
            >
              📋 Copy
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}

export default ShareableCard;

