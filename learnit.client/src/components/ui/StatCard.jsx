import clsx from "clsx";
import Card from "./Card";
import styles from "./ui.module.css";

function StatCard({ label, value, hint, className, children, ...rest }) {
  return (
    <Card className={clsx(styles.statCard, className)} {...rest}>
      {label && <span className={styles.statLabel}>{label}</span>}
      {value && <strong className={styles.statValue}>{value}</strong>}
      {hint && <p className={styles.statHint}>{hint}</p>}
      {children}
    </Card>
  );
}

export default StatCard;
