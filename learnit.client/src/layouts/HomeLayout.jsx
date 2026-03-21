import { Outlet } from "react-router-dom";
import HomeNavbar from "../components/home/HomeNavbar";
import HomeFooter from "../components/home/HomeFooter";

const HomeLayout = () => {
  return (
    <>
      <HomeNavbar />

      <main>
        <Outlet />
      </main>

      <footer>
        <HomeFooter />
      </footer>
    </>
  );
};

export default HomeLayout;
