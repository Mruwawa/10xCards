import { Box, Button, Heading, Input, Stack, Text } from '@chakra-ui/react';
import { useState } from 'react';
import { useAuth } from '../state/auth';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';

export default function Login() {
  const { setToken } = useAuth();
  const nav = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const submit = async () => {
    setError(null);
    try {
      const res = await api.post('/auth/login', { email, password });
      setToken(res.token);
      nav('/generate');
    } catch (e: any) {
      setError(e.message || 'Login failed');
    }
  };
  return (
    <Box maxW="sm" mx="auto">
      <Heading size="md" mb={4}>Logowanie</Heading>
      <Stack spacing={3}>
        <Input placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} />
        <Input placeholder="Hasło" type="password" value={password} onChange={e => setPassword(e.target.value)} />
        {error && <Text color="red.500" fontSize="sm">{error}</Text>}
        <Button colorScheme="blue" onClick={submit}>Zaloguj</Button>
      </Stack>
    </Box>
  );
}
