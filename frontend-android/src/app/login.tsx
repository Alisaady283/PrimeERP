import { Redirect } from "expo-router";
import { isSignedIn } from "@/features/auth/session";
import { LoginPage } from "~/features/auth/LoginPage";

/** كمسار /login في الويب: المسجَّل يذهب إلى الرئيسية */
export default function LoginRoute() {
  return isSignedIn() ? <Redirect href="/" /> : <LoginPage />;
}
