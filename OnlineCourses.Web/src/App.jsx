import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { RequireAuth } from "./components/RequireAuth";
import { CourseDetailsPage } from "./pages/CourseDetailsPage";
import { CoursesPage } from "./pages/CoursesPage";
import { DashboardPage } from "./pages/DashboardPage";
import { FaqPage } from "./pages/FaqPage";
import { LandingPage } from "./pages/LandingPage";
import { LessonPage } from "./pages/LessonPage";
import { LoginPage } from "./pages/LoginPage";
import { ManageCategoriesPage } from "./pages/ManageCategoriesPage";
import { ManageCourseAnalyticsPage } from "./pages/ManageCourseAnalyticsPage";
import { ManageCourseReviewsPage } from "./pages/ManageCourseReviewsPage";
import { ManageCourseStudentsPage } from "./pages/ManageCourseStudentsPage";
import { ManageCoursesPage } from "./pages/ManageCoursesPage";
import { ManageLessonsPage } from "./pages/ManageLessonsPage";
import { ManageSectionsPage } from "./pages/ManageSectionsPage";
import { MyCoursesPage } from "./pages/MyCoursesPage";
import { ProfilePage } from "./pages/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage";
import { StoriesPage } from "./pages/StoriesPage";
import { WhyItPage } from "./pages/WhyItPage";

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<LandingPage />} />
        <Route path="/why-it" element={<WhyItPage />} />
        <Route path="/faq" element={<FaqPage />} />
        <Route path="/stories" element={<StoriesPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/catalog" element={<CoursesPage />} />
        <Route path="/courses" element={<CoursesPage />} />
        <Route path="/courses/:courseId" element={<CourseDetailsPage />} />
        <Route path="/lessons/:sectionId/:lessonId" element={<LessonPage />} />

        <Route element={<RequireAuth />}>
          <Route path="/my-courses" element={<MyCoursesPage />} />
          <Route path="/profile" element={<ProfilePage />} />
        </Route>

        <Route element={<RequireAuth roles={["teacher", "admin"]} />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/manage/courses" element={<ManageCoursesPage />} />
          <Route path="/manage/courses/:courseId/sections" element={<ManageSectionsPage />} />
          <Route path="/manage/sections/:sectionId/lessons" element={<ManageLessonsPage />} />
          <Route path="/manage/courses/:courseId/students" element={<ManageCourseStudentsPage />} />
          <Route path="/manage/courses/:courseId/analytics" element={<ManageCourseAnalyticsPage />} />
        </Route>

        <Route element={<RequireAuth roles={["admin"]} />}>
          <Route path="/manage/categories" element={<ManageCategoriesPage />} />
          <Route path="/manage/courses/:courseId/reviews" element={<ManageCourseReviewsPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/catalog" replace />} />
      </Route>
    </Routes>
  );
}
