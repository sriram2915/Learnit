import React from "react";
import { ErrorMessage } from "./ErrorMessage";
import styles from "./ErrorBoundary.module.css";

class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    console.error("ErrorBoundary caught an error:", error, errorInfo);
    this.setState({ errorInfo });
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div className={styles.container}>
          <ErrorMessage
            title="Application Error"
            error={
              this.state.error?.message ||
              "Something unexpected happened. Please refresh the page."
            }
            onRetry={this.handleReset}
            variant="default"
          />
          {process.env.NODE_ENV === "development" && this.state.errorInfo && (
            <details className={styles.details}>
              <summary>Error Details (Development Only)</summary>
              <pre className={styles.stack}>
                {this.state.error?.stack}
                {"\n\n"}
                {this.state.errorInfo.componentStack}
              </pre>
            </details>
          )}
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;

