import clsx from "clsx";
import styles from "./ui.module.css";

function Field({ label, hint, className, children }) {
  return (
    <div className={clsx(styles.field, className)}>
      {label && <label>{label}</label>}
      {children}
      {hint && <p className={styles.fieldHint}>{hint}</p>}
    </div>
  );
}

export default Field;
