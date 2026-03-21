import clsx from "clsx";
import styles from "./ui.module.css";

function Button({ children, variant = "ghost", className, ...rest }) {
  const classes = clsx(
    styles.button,
    {
      [styles.buttonPrimary]: variant === "primary",
      [styles.buttonGhost]: variant === "ghost",
      [styles.buttonDanger]: variant === "danger",
      [styles.buttonText]: variant === "text",
    },
    className
  );

  return (
    <button className={classes} {...rest}>
      {children}
    </button>
  );
}

export default Button;
