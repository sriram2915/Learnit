import { useContext, useMemo, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";
import styles from "./Layout.module.css";
import Sidebar from "../components/main/Sidebar";
import { AuthContext } from "../context/AuthContext";

const PAGE_META = {
  course: {
    title: "Courses",
    kicker: "Course workspace",
    subtitle: "Create, organize, and track your learning courses.",
  },
  schedule: {
    title: "Schedule",
    kicker: "Study planner",
    subtitle: "Plan and manage your sessions with auto-scheduling.",
  },
  progress: {
    title: "Progress",
    kicker: "Insights",
    subtitle: "Track streaks, hours, and milestones.",
  },
  profile: {
    title: "Profile",
    kicker: "Account",
    subtitle: "Manage your info, preferences, theme, and friends.",
  },
  ai: {
    title: "AI",
    kicker: "AI workspace",
    subtitle: "Chat, compare friends, and draft courses.",
  },
  classrooms: {
    title: "Classrooms",
    kicker: "Learning workspace",
    subtitle: "Stay organized and on track.",
  },
  awards: {
    title: "Awards",
    kicker: "Learning workspace",
    subtitle: "Stay organized and on track.",
  },
};

const Layout = () => {
  const location = useLocation();
  const { user } = useContext(AuthContext);
  const section = location.pathname.split("/")[2] || "dashboard";
  const meta = PAGE_META[section] || {
    title: "Dashboard",
    kicker: "Learning workspace",
    subtitle: "Stay organized and on track.",
  };

  const { displayName, displayRole, initials, email } = useMemo(() => {
    const name = user?.fullName || user?.name || "";
    const derivedInitials = name
      ? name
          .split(" ")
          .filter(Boolean)
          .slice(0, 2)
          .map((n) => n[0]?.toUpperCase())
          .join("")
      : user?.email?.[0]?.toUpperCase() || "U";

    return {
      displayName: name || user?.email || "Your account",
      displayRole: user?.role || "Learner",
      initials: derivedInitials,
      email: user?.email || "",
    };
  }, [user]);

  // Lift sidebar collapsed state to Layout
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className={styles.dashboard}>
      <Sidebar
        collapsed={sidebarCollapsed}
        setCollapsed={setSidebarCollapsed}
      />
      <div
        className={styles.mainArea}
        style={{
          marginLeft: sidebarCollapsed ? 88 : 260,
          transition: "margin-left 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
        }}
      >
        <header className={styles.topbar}>
          <div>
            <p className={styles.kicker}>
              {meta.kicker || "Learning workspace"}
            </p>
            <h1 className={styles.topTitle}>{meta.title || "Dashboard"}</h1>
            {meta.subtitle && (
              <p className={styles.topSubtitle}>{meta.subtitle}</p>
            )}
          </div>

          <div className={styles.topRight}>
            <div className={styles.userChip} title={email}>
              <div className={styles.avatar}>{initials}</div>
              <div>
                <span className={styles.userName}>{displayName}</span>
                <span className={styles.userRole}>{displayRole}</span>
              </div>
            </div>
          </div>
        </header>

        <main className={styles.content}>
          <Outlet />
        </main>

        <footer className={styles.footer}>
          <span>© {new Date().getFullYear()} Learnit AI Planner</span>
          <span>Focused learning made simple</span>
        </footer>
      </div>
    </div>
  );
};

export default Layout;
