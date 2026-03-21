import styles from "./Navbar.module.css";
import { CgProfile } from "react-icons/cg";
import { Link } from "react-router-dom";
import { useState, useRef, useEffect } from "react";
import { IoIosLogOut } from "react-icons/io";
import { useLogout } from "../../hooks/useLogout";

function Navbar() {
  const [open, setOpen] = useState(false);
  const menuRef = useRef();
  const { logout } = useLogout();

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClick(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  const handleLogout = async () => {
    // Close dropdown before logging out
    setOpen(false);
    await logout();
  };

  return (
    <nav className={styles.navbar}>
      <div className={styles.logo}>
        <Link to="/">Learnit</Link>
      </div>

      <ul className={styles.navLinks}>
        <li>
          <Link to="/app/course">Courses</Link>
        </li>
        <li>
          <Link to="/app/schedule">Schedule</Link>
        </li>
        <li>
          <Link to="/app/progress">Progress</Link>
        </li>
        <li>
          <Link to="/app/profile">Profile</Link>
        </li>
      </ul>

      <div className={styles.profileWrapper} ref={menuRef}>
        <button className={styles.profileButton} onClick={() => setOpen(!open)}>
          <CgProfile size={26} />
        </button>

        {open && (
          <div className={styles.dropdown}>
            <Link to="/app/profile">Profile</Link>
            <button className={styles.logoutButton} onClick={handleLogout}>
              <IoIosLogOut size={18} /> Logout
            </button>
          </div>
        )}
      </div>
    </nav>
  );
}

export default Navbar;
