import React, { createContext, useContext, useState, ReactNode, useEffect } from 'react';
import { initToken, getToken as getStoredToken, setToken as setStoredToken } from './authToken';

interface AuthContextValue {
  token: string | null;
  setToken: (t: string | null) => void;
  logout: () => void;
}
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  initToken();
  const [token, setTokenState] = useState<string | null>(() => getStoredToken());
  const setToken = (t: string | null) => {
    setStoredToken(t || null);
    setTokenState(t);
  };
  const logout = () => setToken(null);
  useEffect(() => { /* refresh logic placeholder */ }, []);
  return <AuthContext.Provider value={{ token, setToken, logout }}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('AuthProvider missing');
  return ctx;
};
