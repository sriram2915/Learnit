import { useContext, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";
import { authApi } from "../services";

/**
 * Custom hook for handling logout functionality
 * Ensures logout always succeeds locally even if API call fails
 * Prevents multiple simultaneous logout calls
 */
export function useLogout() {
  const { logout: clearAuth } = useContext(AuthContext);
  const navigate = useNavigate();
  const isLoggingOut = useRef(false);

  const logout = async () => {
    // Prevent multiple simultaneous logout calls
    if (isLoggingOut.current) {
      return;
    }

    isLoggingOut.current = true;

    try {
      // Attempt to call logout API endpoint
      await authApi.logout();
    } catch (err) {
      // If logout API fails, proceed with local logout
      console.warn(
        "Logout API call failed, proceeding with local logout:",
        err.message
      );
    } finally {
      // Always clear local authentication state
      clearAuth();
      // Reset the flag
      isLoggingOut.current = false;
      // Redirect to login page
      navigate("/auth/login", { replace: true });
    }
  };

  return { logout };
}
