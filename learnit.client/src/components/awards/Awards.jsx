import { useState, useEffect } from "react";
import { awardApi } from "../../services";
import styles from "./Awards.module.css";
import { useToast, ToastContainer } from "../ui/index";

const Awards = () => {
  const [awards, setAwards] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [summary, setSummary] = useState(null);
  const { toasts, removeToast, showToast } = useToast();

  useEffect(() => {
    loadAwards();
  }, []);

  const loadAwards = async () => {
    try {
      setLoading(true);
      setError("");
      const data = await awardApi.getAwards();
      setAwards(data.awards || []);
      setSummary(data);

      // Check for new awards
      const checkResult = await awardApi.checkAwards();
      if (checkResult.awards && checkResult.awards.length > 0) {
        showToast(
          `🎉 Congratulations! You earned ${checkResult.awards.length} new award(s)!`,
          "success"
        );
        // Reload awards to show newly earned ones
        const updatedData = await awardApi.getAwards();
        setAwards(updatedData.awards || []);
        setSummary(updatedData);
      }
    } catch (err) {
      console.error("[Awards] Error loading awards:", err);
      setError(err?.message || "Failed to load awards");
    } finally {
      setLoading(false);
    }
  };

  const categories = [
    "all",
    "hours",
    "courses",
    "consistency",
    "longeststreak",
  ];
  const categoryLabels = {
    all: "All Awards",
    hours: "Study Hours",
    courses: "Course Completion",
    consistency: "Consistency Streaks",
    longeststreak: "Longest Streaks",
  };

  const filteredAwards =
    selectedCategory === "all"
      ? awards
      : awards.filter((a) => a.category.toLowerCase() === selectedCategory);

  const earnedCount = awards.filter((a) => a.isEarned).length;
  const totalCount = awards.length;

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading}>Loading awards...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>{error}</div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <ToastContainer toasts={toasts} removeToast={removeToast} />

      <div className={styles.header}>
        <div className={styles.headerText}>
          <h1>Awards & Achievements</h1>
          <p className={styles.subtitle}>
            Track your learning milestones and celebrate your progress!
          </p>
        </div>

        {summary && (
          <div className={styles.summaryInline}>
            <div className={styles.summaryCard}>
              <div className={styles.summaryValue}>{earnedCount}</div>
              <div className={styles.summaryLabel}>Earned</div>
            </div>
            <div className={styles.summaryCard}>
              <div className={styles.summaryValue}>{totalCount}</div>
              <div className={styles.summaryLabel}>Total</div>
            </div>
            <div className={styles.summaryCard}>
              <div className={styles.summaryValue}>
                {totalCount > 0
                  ? Math.round((earnedCount / totalCount) * 100)
                  : 0}
                %
              </div>
              <div className={styles.summaryLabel}>Complete</div>
            </div>
          </div>
        )}
      </div>

      <div className={styles.categoryFilters}>
        {categories.map((cat) => (
          <button
            key={cat}
            className={`${styles.categoryBtn} ${
              selectedCategory === cat ? styles.active : ""
            }`}
            onClick={() => setSelectedCategory(cat)}
          >
            {categoryLabels[cat]}
          </button>
        ))}
      </div>

      <div className={styles.awardsGrid}>
        {filteredAwards.length === 0 ? (
          <div className={styles.empty}>No awards found in this category.</div>
        ) : (
          filteredAwards.map((award) => (
            <div
              key={award.id}
              className={`${styles.awardCard} ${
                award.isEarned ? styles.earned : styles.locked
              }`}
            >
              <div className={styles.awardHeader}>
                <div className={styles.awardIcon} title={award.name}>
                  {/* Only show the first emoji/icon if multiple are present */}
                  <span className={styles.iconGlyph}>
                    {Array.isArray(award.icon)
                      ? String(award.icon[0] || "")
                      : typeof award.icon === "string" &&
                        award.icon.match(/\p{Emoji}/gu)
                      ? award.icon.match(/\p{Emoji}/gu)[0]
                      : typeof award.icon === "string"
                      ? award.icon[0]
                      : award.icon}
                  </span>
                </div>
                <div className={styles.awardInfo}>
                  <h3 className={styles.awardName}>{award.name}</h3>
                  <p className={styles.awardDescription}>{award.description}</p>
                </div>
              </div>
              <div className={styles.awardFooter}>
                {award.isEarned ? (
                  <div className={styles.earnedBadge}>
                    <span className={styles.earnedDate}>
                      Earned {new Date(award.earnedAt).toLocaleDateString()}
                    </span>
                    {award.progressValue && (
                      <span className={styles.progressValue}>
                        {award.progressValue}{" "}
                        {award.category === "hours"
                          ? "hours"
                          : award.category === "courses"
                          ? "courses"
                          : "days"}
                      </span>
                    )}
                  </div>
                ) : (
                  <div className={styles.progressBar}>
                    <div
                      className={styles.progressFill}
                      style={{
                        width: `${Math.min(
                          100,
                          award.progressPercentage || 0
                        )}%`,
                        backgroundColor: award.color || undefined,
                      }}
                    />
                    <span className={styles.progressText}>
                      {award.progressPercentage
                        ? `${Math.round(award.progressPercentage)}%`
                        : "0%"}
                      {award.threshold && ` / ${award.threshold}`}
                    </span>
                  </div>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default Awards;
