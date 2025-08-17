import { Box, Flex, Heading, HStack, Link, Button } from '@chakra-ui/react';
import { NavLink, Route, Routes, useNavigate } from 'react-router-dom';
import Login from './Login';
import Register from './Register';
import Generate from './Generate';
import MyFlashcards from './MyFlashcards';
import Study from './Study';
import Stats from './Stats';
import Account from './Account';
import Landing from './Landing';
import { useAuth } from '../state/auth';

const NavBar = () => {
  const { token, logout } = useAuth();
  const navigate = useNavigate();
  return (
    <Flex bg="gray.800" color="white" px={6} py={3} align="center" justify="space-between">
      <Heading size="md" cursor='pointer'>
        <Link as={NavLink} to="/" _hover={{textDecoration:'none'}}>
          10xCards
        </Link>
      </Heading>
      <HStack spacing={4}>
        {token && <Link as={NavLink} to="/generate">Generuj</Link>}
        {token && <Link as={NavLink} to="/flashcards">Moje fiszki</Link>}
        {token && <Link as={NavLink} to="/study">Nauka</Link>}
        {token && <Link as={NavLink} to="/stats">Statystyki</Link>}
        {token && <Link as={NavLink} to="/account">Konto</Link>}
        {token && <Button size="sm" onClick={() => { logout(); navigate('/'); }}>Wyloguj</Button>}
      </HStack>
    </Flex>
  );
};

export default function App() {
  return (
    <Flex direction="column" minH="100vh">
      <NavBar />
      <Box flex={1} p={6} bg="gray.50">
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/generate" element={<Generate />} />
          <Route path="/flashcards" element={<MyFlashcards />} />
          <Route path="/study" element={<Study />} />
          <Route path="/account" element={<Account />} />
          <Route path="/stats" element={<Stats />} />
          <Route path="*" element={<Landing />} />
        </Routes>
      </Box>
    </Flex>
  );
}
