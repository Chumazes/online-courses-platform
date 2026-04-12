import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { RequireAuth } from "./components/RequireAuth";
import { CourseDetailsPage } from "./pages/CourseDetailsPage";
import { CoursesPage } from "./pages/CoursesPage";
import { LandingPage } from "./pages/LandingPage";
import { LessonPage } from "./pages/LessonPage";
import { LoginPage } from "./pages/LoginPage";
import { ManageCategoriesPage } from "./pages/ManageCategoriesPage";
import { ManageCoursesPage } from "./pages/ManageCoursesPage";
import { ModerationPage } from "./pages/ModerationPage";
import { MyCoursesPage } from "./pages/MyCoursesPage";
import { ProfilePage } from "./pages/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage";

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/courses" element={<CoursesPage />} />
        <Route path="/courses/:courseId" element={<CourseDetailsPage />} />
        <Route path="/lessons/:sectionId/:lessonId" element={<LessonPage />} />

        <Route element={<RequireAuth />}>
          <Route path="/my-courses" element={<MyCoursesPage />} />
          <Route path="/profile" element={<ProfilePage />} />
        </Route>

        <Route element={<RequireAuth roles={["teacher", "admin"]} />}>
          <Route path="/manage/courses" element={<ManageCoursesPage />} />
        </Route>

        <Route element={<RequireAuth roles={["admin"]} />}>
          <Route path="/manage/categories" element={<ManageCategoriesPage />} />
          <Route path="/manage/reviews" element={<ModerationPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/courses" replace />} />
      </Route>
    </Routes>
  );
}

