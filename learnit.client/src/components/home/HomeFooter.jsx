import styles from "./HomeFooter.module.css";
import { FaGithub } from "react-icons/fa6";
import { FaLinkedin } from "react-icons/fa";
import { FaGlobe } from "react-icons/fa";

function HomeFooter() {
  return (
    <footer className={styles.homeFooter}>
      <h1>Learnit</h1>
      <span className={styles.tagline}>
        © 2025 Learnit. All rights reserved.
      </span>
      <Socials />
    </footer>
  );
}

export default HomeFooter;

function Socials() {
  const githubUrl = "https://github.com/Selva-vignesh-7";
  const linkedinUrl = "https://www.linkedin.com/in/selvavignesh/";
  const websiteUrl = "https://github.com/Selva-vignesh-7/learnit.client";

  return (
    <div className={styles.socials}>
      <a
        href={githubUrl}
        target="_blank"
        rel="noopener noreferrer"
        aria-label="GitHub"
        title="GitHub"
      >
        <FaGithub />
      </a>
      <a
        href={linkedinUrl}
        target="_blank"
        rel="noopener noreferrer"
        aria-label="LinkedIn"
        title="LinkedIn"
      >
        <FaLinkedin />
      </a>
      <a
        href={websiteUrl}
        target="_blank"
        rel="noopener noreferrer"
        aria-label="Project"
        title="Project"
      >
        <FaGlobe />
      </a>
    </div>
  );
}
