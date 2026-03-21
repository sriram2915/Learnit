import { createBrowserRouter } from "react-router-dom";

import LandingPage from "./components/home/LandingPage.jsx";

import Signin from "./components/auth/Signin.jsx";
import Signup from "./components/auth/Signup.jsx";

import Course from "./components/main/Course.jsx";
import CourseDetails from "./components/course/CourseDetails.jsx";
import Schedule from "./components/main/Schedule.jsx";
import Progress from "./components/main/Progress.jsx";
import Profile from "./components/main/Profile.jsx";
import Ai from "./components/main/Ai.jsx";
import ClassroomList from "./components/classroom/ClassroomList.jsx";
import ClassroomDetails from "./components/classroom/ClassroomDetails.jsx";
import Awards from "./components/awards/Awards.jsx";

import Layout from "./layouts/Layout.jsx";
import HomeLayout from "./layouts/HomeLayout.jsx";
import AuthLayout from "./layouts/AuthLayout.jsx";
import RequireAuth from "./components/auth/RequireAuth.jsx";

const router = createBrowserRouter([
  // Public Home Pages
  {
    path: "/",
    element: <HomeLayout />,
    children: [{ index: true, element: <LandingPage /> }],
  },

  // Auth Pages (login / register)
  {
    path: "/auth",
    element: <AuthLayout />,
    children: [
      { path: "login", element: <Signin /> },
      { path: "register", element: <Signup /> },
    ],
  },

  // Protected Main App Pages
  {
    path: "/app",
    element: (
      <RequireAuth>
        <Layout />
      </RequireAuth>
    ),
    children: [
      { path: "course", element: <Course /> },
      { path: "course/:id", element: <CourseDetails /> },
      { path: "schedule", element: <Schedule /> },
      { path: "progress", element: <Progress /> },
      { path: "profile", element: <Profile /> },
      { path: "awards", element: <Awards /> },
      { path: "ai", element: <Ai /> },
      { path: "classrooms", element: <ClassroomList /> },
      { 
        path: "classrooms/:id", 
        element: <ClassroomDetails />,
        errorElement: <div style={{ padding: "2rem", textAlign: "center" }}>
          <h2>Error loading classroom</h2>
          <p>The classroom you're looking for doesn't exist or you don't have access to it.</p>
          <button onClick={() => window.location.href = "/app/classrooms"}>Go back to Classrooms</button>
        </div>
      },
    ],
  },
]);

export default router;
