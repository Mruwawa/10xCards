import { Box, Button, Heading, Text, VStack, HStack, Spinner, useDisclosure, AlertDialog, AlertDialogOverlay, AlertDialogContent, AlertDialogHeader, AlertDialogBody, AlertDialogFooter, Input } from '@chakra-ui/react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { useAuth } from '../state/auth';

interface Overview { id:string; email:string; createdAt:string; flashcardsTotal:number; flashcardsAI:number; flashcardsManual:number; }

export default function Account() {
  const [data, setData] = useState<Overview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string|null>(null);
  const { logout } = useAuth();
  const navigate = useNavigate();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement | null>(null);
  const [confirm, setConfirm] = useState('');
  const load = async () => {
    setLoading(true); setError(null);
    try { const d = await api.get('/account'); setData(d); } catch (e:any) { setError(e.message); }
    finally { setLoading(false); }
  };
  useEffect(() => { load(); }, []);
  const del = async () => {
    if (confirm !== 'USUN') return; // extra guard
    try {
      await api.del('/account');
      logout();
      onClose();
      navigate('/login');
    } catch (e:any) {
      setError(e.message);
    }
  };
  return (
    <VStack align="stretch" spacing={6}>
      <Heading size="md">Konto</Heading>
      {loading && <Spinner />}
      {error && <Text color="red.500">{error}</Text>}
      {data && !loading && (
        <Box p={4} borderWidth="1px" borderRadius="md" bg="white">
          <VStack align="start" spacing={2}>
            <Text><b>Email:</b> {data.email}</Text>
            <Text><b>Utworzono:</b> {new Date(data.createdAt).toLocaleString()}</Text>
            <Text><b>Fiszki razem:</b> {data.flashcardsTotal}</Text>
            <HStack><Text><b>AI:</b> {data.flashcardsAI}</Text><Text><b>Manualne:</b> {data.flashcardsManual}</Text></HStack>
          </VStack>
        </Box>
      )}
      <Box>
        <Heading size="sm" mb={2}>Usunięcie konta</Heading>
        <Text fontSize="sm" mb={2}>Operacja nieodwracalna – usuwane są konto, fiszki, logi powtórek, sesje generowania.</Text>
        <Button colorScheme="red" variant="outline" onClick={onOpen}>Usuń konto</Button>
      </Box>
      <AlertDialog isOpen={isOpen} onClose={onClose} leastDestructiveRef={cancelRef}>
        <AlertDialogOverlay />
        <AlertDialogContent>
          <AlertDialogHeader fontSize='lg' fontWeight='bold'>Potwierdź usunięcie</AlertDialogHeader>
          <AlertDialogBody>
            Wpisz "USUN" aby potwierdzić. Tej operacji nie można cofnąć.
            <Input mt={3} value={confirm} onChange={e=>setConfirm(e.target.value)} placeholder='USUN' />
          </AlertDialogBody>
          <AlertDialogFooter>
            <Button ref={cancelRef} onClick={onClose}>Anuluj</Button>
            <Button colorScheme='red' ml={3} isDisabled={confirm !== 'USUN'} onClick={del}>Usuń</Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </VStack>
  );
}
