import { useState, useEffect } from "react";
import { FiCheckCircle, FiX, FiAlertCircle, FiInfo } from "react-icons/fi";
import styles from "./Toast.module.css";

/**
 * Toast Notification Component
 * Shows temporary success/error/info messages
 */
export function Toast({ message, type = "success", duration = 3000, onClose }) {
  const [isVisible, setIsVisible] = useState(true);

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsVisible(false);
      setTimeout(() => onClose?.(), 300); // Wait for fade out animation
    }, duration);

    return () => clearTimeout(timer);
  }, [duration, onClose]);

  if (!isVisible) return null;

  const icons = {
    success: <FiCheckCircle />,
    error: <FiAlertCircle />,
    info: <FiInfo />,
  };

  return (
    <div className={`${styles.toast} ${styles[type]} ${isVisible ? styles.show : ""}`}>
      <div className={styles.icon}>{icons[type] || icons.success}</div>
      <span className={styles.message}>{message}</span>
      <button className={styles.closeBtn} onClick={() => {
        setIsVisible(false);
        setTimeout(() => onClose?.(), 300);
      }} aria-label="Close">
        <FiX />
      </button>
    </div>
  );
}

/**
 * Toast Container - manages multiple toasts
 */
export function ToastContainer({ toasts = [], removeToast }) {
  if (!toasts || toasts.length === 0) return null;
  
  return (
    <div className={styles.container}>
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          message={toast.message}
          type={toast.type}
          duration={toast.duration}
          onClose={() => removeToast?.(toast.id)}
        />
      ))}
    </div>
  );
}

/**
 * useToast Hook - easy toast management
 */
export function useToast() {
  const [toasts, setToasts] = useState([]);

  const showToast = (message, type = "success", duration = 3000) => {
    const id = Date.now() + Math.random();
    setToasts((prev) => [...prev, { id, message, type, duration }]);
    return id;
  };

  const removeToast = (id) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  return {
    toasts,
    showToast,
    removeToast,
    success: (message, duration) => showToast(message, "success", duration),
    error: (message, duration) => showToast(message, "error", duration),
    info: (message, duration) => showToast(message, "info", duration),
  };
}

export default Toast;


