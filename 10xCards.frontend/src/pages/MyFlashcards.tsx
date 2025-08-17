import { Box, Button, Heading, HStack, IconButton, Input, SimpleGrid, Text, VStack, useDisclosure, AlertDialog, AlertDialogOverlay, AlertDialogContent, AlertDialogHeader, AlertDialogBody, AlertDialogFooter, Link, Divider, Flex, Tooltip, useToast } from '@chakra-ui/react';
import { Link as RouterLink } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { api } from '../services/api';
import { DeleteIcon } from '@chakra-ui/icons';

interface Flashcard { id: string; front: string; back: string; nextReview?: string | null; intervalDays?: number; repetitions?: number; }

export default function MyFlashcards() {
  const [cards, setCards] = useState<Flashcard[]>([]);
  const [front, setFront] = useState('');
  const [back, setBack] = useState('');
  const [search, setSearch] = useState('');
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [pendingDelete, setPendingDelete] = useState<Flashcard | null>(null);
  const cancelRef = useState<HTMLButtonElement | null>(null)[0];
  const toast = useToast();
  const load = async () => {
    const data = await api.get('/flashcards');
    setCards(data);
  };
  useEffect(() => { load(); }, []);
  const create = async () => {
    const c = await api.post('/flashcards', { front, back });
  setCards((prev: Flashcard[]) => [c, ...prev]);
    setFront(''); setBack('');
  };
  const save = async (id: string, front: string, back: string) => {
    const updated = await api.put('/flashcards/' + id, { front, back });
  setCards((prev: Flashcard[]) => prev.map((c: Flashcard) => c.id === id ? updated : c));
    toast({ title: 'Zapisano', status: 'success', duration: 1500, isClosable: true });
  };
  const askDelete = (card: Flashcard) => {
    setPendingDelete(card);
    onOpen();
  };
  const confirmDelete = async () => {
    if (!pendingDelete) return;
    await api.del('/flashcards/' + pendingDelete.id);
  setCards((prev: Flashcard[]) => prev.filter((c: Flashcard) => c.id !== pendingDelete.id));
    setPendingDelete(null);
    onClose();
  };
  const filtered = !search ? cards : cards.filter((c: Flashcard) => (c.front + ' ' + c.back).toLowerCase().includes(search.toLowerCase()));
  const bulkResetAll = async () => {
    await api.post('/flashcards/reset/all', {});
    toast({ title: 'Zresetowano wszystkie powtórki', status: 'info', duration: 1800, isClosable: true });
    load();
  };
  return (
    <VStack align="stretch" spacing={8}>
      {/* Sekcja dodawania nowej fiszki */}
      <Box p={5} borderWidth='1px' borderRadius='lg' bg='white'>
        <Heading size="md" mb={4}>Dodaj nową fiszkę</Heading>
        <VStack align='stretch' spacing={3}>
          <HStack>
            <Input placeholder="Przód" value={front} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFront(e.target.value)} />
            <Input placeholder="Tył" value={back} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setBack(e.target.value)} />
            <Button colorScheme="blue" onClick={create} isDisabled={!front || !back}>Dodaj</Button>
          </HStack>
          <Flex fontSize='sm' justify='space-between' flexWrap='wrap' gap={2}>
            <Text color='gray.600'>Możesz też wygenerować fiszki automatycznie.</Text>
            <Link as={RouterLink} color='blue.600' to='/generate' fontWeight='semibold'>Przejdź do generowania →</Link>
          </Flex>
        </VStack>
      </Box>
      <Divider />
      {/* Sekcja listy fiszek */}
      <Box>
        <Heading size="md" mb={3}>Moje fiszki</Heading>
        <HStack mb={4} spacing={3} align="stretch" flexWrap="wrap">
          <Input flex={1} placeholder="Szukaj (przód / tył)" value={search} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearch(e.target.value)} />
          <Tooltip label="Zresetuj powtórki wszystkich fiszek (uwaga)"><Button variant="outline" size="sm" onClick={bulkResetAll}>Reset wszystkich</Button></Tooltip>
        </HStack>
        <Text mb={3} fontSize="sm" color="gray.600">Wyświetlono {filtered.length} / {cards.length}</Text>
        <SimpleGrid columns={{base:1, md:2}} spacing={4}>
          {filtered.map((c: Flashcard) => <EditableCard key={c.id} card={c} onSave={save} onDelete={askDelete} />)}
        </SimpleGrid>
      </Box>
      <AlertDialog isOpen={isOpen} onClose={()=>{ setPendingDelete(null); onClose(); }} leastDestructiveRef={cancelRef}>
        <AlertDialogOverlay />
        <AlertDialogContent>
          <AlertDialogHeader fontSize='lg' fontWeight='bold'>Usuń fiszkę</AlertDialogHeader>
          <AlertDialogBody>
            Czy na pewno chcesz usunąć tę fiszkę? Operacji nie można cofnąć.
            <Box mt={3} p={3} borderWidth='1px' borderRadius='md' bg='gray.50'>
              <Text fontWeight='bold'>{pendingDelete?.front}</Text>
              <Text fontSize='sm' color='gray.600'>{pendingDelete?.back}</Text>
            </Box>
          </AlertDialogBody>
          <AlertDialogFooter>
            <Button onClick={()=>{ setPendingDelete(null); onClose(); }}>Anuluj</Button>
            <Button colorScheme='red' ml={3} onClick={confirmDelete}>Usuń</Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </VStack>
  );
}

function formatNext(next?: string | null) {
  if (!next) return '—';
  try { const d = new Date(next); return d.toLocaleString(); } catch { return next; }
}

function EditableCard({ card, onSave, onDelete }: { card: Flashcard; onSave: (id:string,f:string,b:string)=>void; onDelete:(card:Flashcard)=>void; }) {
  const [front, setFront] = useState(card.front);
  const [back, setBack] = useState(card.back);
  const [dirty, setDirty] = useState(false);
  const [resetting, setResetting] = useState(false);
  const toast = useToast();
  const resetScheduling = async () => {
    setResetting(true);
    try {
      await api.post(`/flashcards/${card.id}/reset`, {});
      toast({ title: 'Zresetowano powtórki', status: 'info', duration: 1500, isClosable: true });
    } finally { setResetting(false); }
  };
  return (
    <Box p={3} borderWidth="1px" borderRadius="md" bg="white">
      <VStack align="stretch" spacing={2}>
        <Input value={front} onChange={(e: React.ChangeEvent<HTMLInputElement>) => { setFront(e.target.value); setDirty(true); }} />
        <Input value={back} onChange={(e: React.ChangeEvent<HTMLInputElement>) => { setBack(e.target.value); setDirty(true); }} />
  <Text fontSize="xs" color="gray.500">Następna powtórka: {formatNext(card.nextReview)}</Text>
  <HStack justify="space-between" spacing={2}>
          <HStack>
            <Button size="sm" colorScheme="green" onClick={() => { onSave(card.id, front, back); setDirty(false); }} isDisabled={!dirty}>Zapisz</Button>
            <Tooltip label="Zresetuj powtórki (ustawi do natychmiastowego powtórzenia)">
              <Button size="sm" variant="outline" onClick={resetScheduling} isLoading={resetting}>Reset</Button>
            </Tooltip>
          </HStack>
          <IconButton aria-label="Usuń" icon={<DeleteIcon />} size="sm" colorScheme="red" onClick={() => onDelete(card)} />
        </HStack>
      </VStack>
    </Box>
  );
}
