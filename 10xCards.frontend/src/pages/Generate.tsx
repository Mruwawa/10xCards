import { Box, Button, Heading, Textarea, VStack, Text, SimpleGrid, HStack, Spinner, Tag, TagLabel, Input, Textarea as CkTextarea, IconButton, Tooltip, useToast, useDisclosure, Modal, ModalOverlay, ModalContent, ModalHeader, ModalBody, ModalFooter } from '@chakra-ui/react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { CheckIcon, CloseIcon, EditIcon, RepeatIcon } from '@chakra-ui/icons';

type SuggestionStatus = 'pending' | 'accepted' | 'rejected';
interface SuggestionItem {
  front: string;
  back: string;
  editedFront: string;
  editedBack: string;
  status: SuggestionStatus;
  editing: boolean;
}

export default function Generate() {
  const [text, setText] = useState('');
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [suggestions, setSuggestions] = useState<SuggestionItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const toast = useToast();
  const navigate = useNavigate();
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [savedCount, setSavedCount] = useState<number>(0);

  const generate = async () => {
    setLoading(true); setMessage(null); setSessionId(null); setSuggestions([]);
    try {
      const res = await api.post('/flashcards/generate', { text });
      setSessionId(res.sessionId);
      const mapped: SuggestionItem[] = (res.suggestions || []).map((s: any) => ({
        front: s.front,
        back: s.back,
        editedFront: s.front,
        editedBack: s.back,
        status: 'pending',
        editing: false
      }));
      setSuggestions(mapped);
    } catch (e: any) {
      setMessage(e.message);
    } finally { setLoading(false); }
  };

  const toggleStatus = (idx: number, status: SuggestionStatus) => {
    setSuggestions((prev: SuggestionItem[]) => prev.map((s: SuggestionItem, i: number) => i===idx ? { ...s, status } : s));
  };
  const toggleEditing = (idx: number) => {
    setSuggestions((prev: SuggestionItem[]) => prev.map((s: SuggestionItem, i: number) => i===idx ? { ...s, editing: !s.editing } : s));
  };
  const resetEdits = (idx: number) => {
    setSuggestions((prev: SuggestionItem[]) => prev.map((s: SuggestionItem, i: number) => i===idx ? { ...s, editedFront: s.front, editedBack: s.back } : s));
  };
  const updateField = (idx: number, field: 'editedFront'|'editedBack', val: string) => {
    setSuggestions((prev: SuggestionItem[]) => prev.map((s: SuggestionItem, i: number) => i===idx ? { ...s, [field]: val } as SuggestionItem : s));
  };

  const saveAccepted = async () => {
    if (!sessionId) return;
    const accepted = suggestions.filter((s: SuggestionItem) => s.status === 'accepted').map((s: SuggestionItem) => ({ front: s.editedFront.trim(), back: s.editedBack.trim() }));
    if (!accepted.length) { setMessage('Brak zaakceptowanych kart'); return; }
    setSaving(true); setMessage(null);
    try {
      const res = await api.post('/flashcards/generate/accept', { sessionId, cards: accepted });
      const created = res.accepted as number;
      setSavedCount(created);
      // wyczyść wszystkie sugestie (zaakceptowane + niezaakceptowane) aby uniknąć duplikacji
      setSuggestions([]);
      setSessionId(null);
      setMessage(null);
      toast.closeAll();
      onOpen();
    } catch (e:any) { setMessage(e.message); }
    finally { setSaving(false); }
  };

  const acceptedCount = suggestions.filter((s: SuggestionItem) => s.status==='accepted').length;
  const rejectedCount = suggestions.filter((s: SuggestionItem) => s.status==='rejected').length;

  return (
    <VStack align="stretch" spacing={6}>
      <Box>
        <Heading size="md" mb={2}>Generuj fiszki z tekstu</Heading>
  <Textarea placeholder="Wklej tekst (1000-10000 znaków)" value={text} onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setText(e.target.value)} rows={8} />
        <HStack mt={3} wrap="wrap" spacing={3}>
          <Button colorScheme="blue" onClick={generate} isDisabled={text.length < 1000 || text.length > 10000 || loading}>Generuj</Button>
          {loading && <Spinner />}
          <Text fontSize="sm" color="gray.500">{text.length} znaków</Text>
          {sessionId && suggestions.length>0 && (
            <HStack spacing={2}>
              <Tag size="sm" colorScheme="green"><TagLabel>Akceptowane: {acceptedCount}</TagLabel></Tag>
              <Tag size="sm" colorScheme="red"><TagLabel>Odrzucone: {rejectedCount}</TagLabel></Tag>
              <Tag size="sm"><TagLabel>Oczekujące: {suggestions.length - acceptedCount - rejectedCount}</TagLabel></Tag>
            </HStack>
          )}
        </HStack>
      </Box>
      {suggestions.length > 0 && (
        <Box>
          <Heading size="sm" mb={2}>Sugestie ({suggestions.length}) – edytuj i oznacz, które chcesz zapisać</Heading>
          <SimpleGrid columns={{base:1, md:2}} spacing={4}>
            {suggestions.map((s, i) => {
              const borderColor = s.status==='accepted' ? 'green.400' : s.status==='rejected' ? 'red.400' : 'gray.200';
              return (
                <Box key={i} p={3} borderWidth="2px" borderColor={borderColor} borderRadius="md" bg="white" _hover={{ shadow:'sm' }}>
                  <HStack justify="space-between" mb={2}>
                    <HStack spacing={1}>
                      <Tooltip label="Akceptuj"><IconButton aria-label='Akceptuj' size='sm' icon={<CheckIcon />} variant={s.status==='accepted'?'solid':'outline'} colorScheme='green' onClick={()=>toggleStatus(i,'accepted')} /></Tooltip>
                      <Tooltip label="Odrzuć"><IconButton aria-label='Odrzuć' size='sm' icon={<CloseIcon />} variant={s.status==='rejected'?'solid':'outline'} colorScheme='red' onClick={()=>toggleStatus(i,'rejected')} /></Tooltip>
                      <Tooltip label={s.editing? 'Zakończ edycję':'Edytuj'}><IconButton aria-label='Edytuj' size='sm' icon={<EditIcon />} variant={s.editing?'solid':'outline'} onClick={()=>toggleEditing(i)} /></Tooltip>
                      {s.editing && <Tooltip label='Przywróć oryginał'><IconButton aria-label='Przywróć oryginał' size='sm' icon={<RepeatIcon />} onClick={()=>resetEdits(i)} /></Tooltip>}
                    </HStack>
                    <Tag size='sm' colorScheme={s.status==='accepted'?'green': s.status==='rejected'?'red':'gray'}>
                      {s.status==='accepted' && 'zaakceptowana'}
                      {s.status==='rejected' && 'odrzucona'}
                      {s.status==='pending' && 'oczekuje'}
                    </Tag>
                  </HStack>
                  {s.editing ? (
                    <VStack align='stretch' spacing={2}>
                      <Input value={s.editedFront} onChange={(e: React.ChangeEvent<HTMLInputElement>)=>updateField(i,'editedFront', e.target.value)} placeholder='Przód' />
                      <CkTextarea value={s.editedBack} onChange={(e: React.ChangeEvent<HTMLTextAreaElement>)=>updateField(i,'editedBack', e.target.value)} placeholder='Tył' rows={3} />
                    </VStack>
                  ) : (
                    <>
                      <Text fontWeight="bold" mb={1}>{s.editedFront}</Text>
                      <Text fontSize="sm" color="gray.600">{s.editedBack}</Text>
                    </>
                  )}
                </Box>
              );
            })}
          </SimpleGrid>
          <HStack mt={4}>
            <Button colorScheme="green" onClick={saveAccepted} isDisabled={!acceptedCount || saving}>
              {saving? <Spinner size='sm' mr={2}/> : null}
              Zapisz zaakceptowane ({acceptedCount})
            </Button>
            <Button variant='outline' onClick={()=>setSuggestions((prev: SuggestionItem[])=>prev.map((s: SuggestionItem)=>({...s,status:'accepted'})))} isDisabled={!suggestions.length}>Akceptuj wszystkie</Button>
            <Button variant='outline' onClick={()=>setSuggestions((prev: SuggestionItem[])=>prev.map((s: SuggestionItem)=>({...s,status:'pending'})))} isDisabled={!suggestions.length}>Zresetuj statusy</Button>
          </HStack>
        </Box>
      )}
      {message && <Text color={message.startsWith('Utworzono')? 'green.600':'purple.600'}>{message}</Text>}
      <Modal isOpen={isOpen} onClose={() => { onClose(); }} isCentered size='lg'>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Zapisano {savedCount} fiszek</ModalHeader>
          <ModalBody>
            <Text mb={4}>Co chcesz zrobić dalej?</Text>
            <VStack align='stretch' spacing={3}>
              <Button colorScheme='green' onClick={()=>{ onClose(); navigate('/study'); }}>Przejdź do nauki</Button>
              <Button colorScheme='blue' onClick={()=>{ onClose(); navigate('/flashcards'); }}>Zobacz moje fiszki</Button>
              <Button variant='outline' onClick={()=>{ onClose(); setText(''); setMessage(null); }}>Wygeneruj więcej</Button>
            </VStack>
          </ModalBody>
          <ModalFooter>
            <Button onClick={()=>{ onClose(); }} variant='ghost'>Zamknij</Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </VStack>
  );
}
