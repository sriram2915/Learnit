import styles from "./HomeNavbar.module.css";

import { Link } from "react-router-dom";

function HomeNavbar() {
  return (
    <nav className={styles.homeNavbar}>
      <h1 className={styles.logo}>
        <a href="/">Learnit</a>
      </h1>
      <ul className={styles.center}>
        <li>
          <a href="#features">Features</a>
        </li>
        <li>
          <a href="#about-us">About us</a>
        </li>
        <li>
          <a href="#contact">Contact</a>
        </li>
      </ul>
      <ul className={styles.right}>
        <li className={styles.signinButton}>
          <Link to="/auth/login">Sign in</Link>
        </li>
        <li className={styles.signupButton}>
          <Link to="/auth/register">Sign up</Link>
        </li>
      </ul>
    </nav>
  );
}

export default HomeNavbar;
