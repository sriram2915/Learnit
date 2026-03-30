import { useEffect, useMemo, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import styles from "./Sidebar.module.css";
import { useLogout } from "../../hooks/useLogout";
import { scheduleApi } from "../../services";

import { FiChevronLeft, FiChevronRight } from "react-icons/fi";
import { IoIosLogOut } from "react-icons/io";
import { MdOutlineMenuBook } from "react-icons/md";
import { AiOutlineSchedule } from "react-icons/ai";
import { BsGraphUp, BsChatDots } from "react-icons/bs";
import { CgProfile } from "react-icons/cg";
import { FaUsers } from "react-icons/fa";
import { FaTrophy } from "react-icons/fa";

const Sidebar = ({ collapsed, setCollapsed }) => {
  const location = useLocation();
  const { logout } = useLogout();
  const [scheduleLoading, setScheduleLoading] = useState(true);
  const [scheduleError, setScheduleError] = useState("");
  const [weekStats, setWeekStats] = useState({ scheduled: 0, completed: 0 });

  useEffect(() => {
    const computeWeeklyStatsFromEvents = (events) => {
      const now = new Date();
      const today = new Date(now);
      const dayOfWeek = today.getDay();
      const daysFromMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

      const weekStart = new Date(today);
      weekStart.setDate(today.getDate() - daysFromMonday);
      weekStart.setHours(0, 0, 0, 0);

      const weekEnd = new Date(weekStart);
      weekEnd.setDate(weekStart.getDate() + 7);
      weekEnd.setHours(23, 59, 59, 999);

      let scheduled = 0;
      let completed = 0;

      (events || []).forEach((e) => {
        if (!e?.courseModuleId) return;

        const start = new Date(e.startUtc ?? e.start);
        const end = e.endUtc || e.end
          ? new Date(e.endUtc ?? e.end)
          : new Date(start.getTime() + 60 * 60 * 1000);

        if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return;

        const eventEnd = end > start ? end : start;
        if (eventEnd < weekStart || start > weekEnd) return;

        const eventStartInWeek = start < weekStart ? weekStart : start;
        const eventEndInWeek = eventEnd > weekEnd ? weekEnd : eventEnd;

        const hours = Math.max(
          0.25,
          (eventEndInWeek - eventStartInWeek) / (1000 * 60 * 60)
        );

        scheduled += hours;
        if (e?.courseModule?.isCompleted || eventEnd <= now) {
          completed += hours;
        }
      });

      return {
        scheduled: Math.round(scheduled * 10) / 10,
        completed: Math.round(completed * 10) / 10,
      };
    };

    const fetchWeekStats = async () => {
      try {
        setScheduleLoading(true);
        setScheduleError("");
        const events = await scheduleApi.getScheduleEvents();
        setWeekStats(computeWeeklyStatsFromEvents(events));
      } catch (err) {
        console.error("[Sidebar] Error fetching progress stats:", err);
        setScheduleError(err?.message || "Failed to load progress");
        setWeekStats({ scheduled: 0, completed: 0 });
      } finally {
        setScheduleLoading(false);
      }
    };

    fetchWeekStats();

    // Refresh every minute to update completion status
    const interval = setInterval(fetchWeekStats, 60000);
    return () => clearInterval(interval);
  }, []);

  const formatHours = (hours) => {
    if (hours === null || hours === undefined) return "--";
    const rounded = Math.round(hours * 10) / 10;
    const display = Number.isInteger(rounded)
      ? rounded.toFixed(0)
      : rounded.toFixed(1);
    return `${display} hrs`;
  };

  const targetHours = weekStats.scheduled;
  const completedHours = weekStats.completed;
  const completionPct = targetHours
    ? Math.min(100, Math.round((completedHours / targetHours) * 100))
    : 0;

  const targetLabel = scheduleLoading ? "Loading..." : formatHours(targetHours);

  const completionLabel = scheduleLoading
    ? "Syncing schedule"
    : `${completionPct}% complete`;

  const menuItems = useMemo(
    () => [
      {
        path: "/app/course",
        label: "Courses",
        icon: <MdOutlineMenuBook size={22} />,
        activeKey: "course",
      },
      {
        path: "/app/schedule",
        label: "Schedule",
        icon: <AiOutlineSchedule size={22} />,
        activeKey: "schedule",
      },
      {
        path: "/app/progress",
        label: "Progress",
        icon: <BsGraphUp size={22} />,
        activeKey: "progress",
      },
      {
        path: "/app/ai",
        label: "Ask AI",
        icon: <BsChatDots size={22} />,
        activeKey: "ai",
      },
      {
        path: "/app/classrooms",
        label: "Classrooms",
        icon: <FaUsers size={22} />,
        activeKey: "classrooms",
      },
      {
        path: "/app/awards",
        label: "Awards",
        icon: <FaTrophy size={22} />,
        activeKey: "awards",
      },
      {
        path: "/app/profile",
        label: "Profile",
        icon: <CgProfile size={22} />,
        activeKey: "profile",
      },
    ],
    []
  );

  return (
    <aside
      className={`${styles.sidebar} ${collapsed ? styles.collapsed : ""}`}
      aria-label="Primary"
    >
      <div className={styles.brandingRow}>
        <div className={styles.branding}>
          <p className={styles.brandTitle}>{collapsed ? "L" : "Learnit"}</p>
          {!collapsed && <span className={styles.brandTag}>Study hub</span>}
        </div>
        <button
          className={styles.toggleBtn}
          onClick={() => setCollapsed((prev) => !prev)}
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          type="button"
        >
          {collapsed ? (
            <FiChevronRight size={18} />
          ) : (
            <FiChevronLeft size={18} />
          )}
        </button>
      </div>
      <div className={styles.sectionLabel}>Navigate</div>
      <nav className={styles.menu}>
        {menuItems.map((item) => {
          const isActive = location.pathname.includes(item.activeKey);
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`${styles.menuItem} ${isActive ? styles.active : ""}`}
            >
              <span className={styles.icon}>{item.icon}</span>
              {!collapsed && <span className={styles.label}>{item.label}</span>}
            </Link>
          );
        })}
      </nav>
      <div className={styles.sidebarBottom}>
        {!collapsed && (
          <>
            <div className={styles.focusCardWrap}>
              <div className={styles.sectionLabel}>Focus</div>
              <div className={styles.focusCard}>
                <div className={styles.focusTopRow}>
                  <span className={styles.focusLabel}>Week target</span>
                  <strong className={styles.focusValue}>{targetLabel}</strong>
                </div>
                <div className={styles.progressTrack}>
                  <span style={{ width: `${completionPct}%` }} />
                </div>
                <small className={styles.focusSub}>{completionLabel}</small>
                {scheduleError && (
                  <small className={styles.errorText}>
                    Schedule data unavailable
                  </small>
                )}
              </div>
            </div>
          </>
        )}
        <div className={styles.footerSection}>
          <button
            className={styles.logoutBtn}
            type="button"
            onClick={logout}
            aria-label="Logout"
          >
            <span className={styles.icon}>
              <IoIosLogOut size={20} />
            </span>
            {!collapsed && "Logout"}
          </button>
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;
