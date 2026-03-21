import clsx from "clsx";
import styles from "./ui.module.css";

function Card({ children, className, ...rest }) {
  return (
    <div className={clsx(styles.card, className)} {...rest}>
      {children}
    </div>
  );
}

export default Card;
