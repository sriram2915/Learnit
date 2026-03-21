import { Outlet } from "react-router-dom";
import HomeFooter from "../components/home/HomeFooter.jsx";

const AuthLayout = () => {
  return (
    <>
      <main>
        <Outlet />
      </main>

      <footer>
        <HomeFooter />
      </footer>
    </>
  );
};

export default AuthLayout;
