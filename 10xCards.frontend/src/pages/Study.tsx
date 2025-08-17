import { Box, Button, Heading, Text, VStack, HStack, Tooltip, Divider, Flex, Tag, TagLabel, Progress } from '@chakra-ui/react';
import { useEffect, useState } from 'react';
import { api } from '../services/api';

interface StudyCard { id: string; front: string; }
// Removed detailed review result UI (next review info) per user request

export default function Study() {
  const [current, setCurrent] = useState<StudyCard | null>(null);
  const [revealed, setRevealed] = useState(false);
  const [back, setBack] = useState<string | null>(null); // fetched on demand via full list
  const [all, setAll] = useState<any[] | null>(null);
  const [learningQueue, setLearningQueue] = useState<StudyCard[]>([]); // cards to immediately retry after low quality
  const [answered, setAnswered] = useState(0);
  const [correct, setCorrect] = useState(0);
  const [uniqueReviewedIds, setUniqueReviewedIds] = useState<Set<string>>(new Set());
  const [initialDueTotal, setInitialDueTotal] = useState<number | null>(null);
  const [dailyStats, setDailyStats] = useState<any | null>(null);
  // Removed lastResult + toast (user requested to hide these messages)
  const loadNext = async () => {
    // If learning queue has items due (time), pop first
    if (learningQueue.length > 0) {
      const [next, ...rest] = learningQueue;
      setLearningQueue(rest);
      setCurrent(next);
      setRevealed(false);
      return;
    }
    const res = await fetchNext();
    setCurrent(res);
    setRevealed(false);
  };
  const fetchNext = async () => {
    try {
      const r = await api.get('/study/next');
      return r; // may be null if 204? wrapper currently throws; improve later
    } catch (e) { return null; }
  };
  const reveal = async () => {
    if (!current) return;
    let list = all;
    if (!list) {
      try {
        list = await api.get('/flashcards');
        setAll(list);
      } catch {
        list = [];
      }
    }
    const found = list?.find((c: any) => c.id === current.id);
    setBack(found?.back ?? '[brak treści]');
    setRevealed(true);
  };
  const review = async (q: number) => {
    if (!current) return;
    try { await api.post('/study/review', { flashcardId: current.id, quality: q }); } catch {}
    setAnswered((a:number) => a + 1);
    if (q >= 3) setCorrect((c:number) => c + 1); else {
      // push back into learning queue (clone) for immediate retry
      setLearningQueue((qs:StudyCard[]) => {
        if (qs.find(x => x.id === current.id)) return qs; // already queued
        if (qs.length >= 5) return qs; // cap
        return [...qs, { id: current.id, front: current.front }];
      });
    }
  setUniqueReviewedIds((prev: Set<string>) => {
      if (prev.has(current.id)) return prev;
      const clone = new Set(prev);
      clone.add(current.id);
      return clone;
    });
    // Refresh daily stats lazily every 5 answers
    if ((answered + 1) % 5 === 0) { fetchDailyStats(); }
    await loadNext();
  };
  const fetchDailyStats = async () => {
    try { const ds = await api.get('/stats/study/today'); setDailyStats(ds); } catch {}
  };

  // Initial load: fetch all and compute due set for progress baseline
  useEffect(() => {
    const init = async () => {
      try {
        const list = await api.get('/flashcards');
        setAll(list);
        const now = new Date();
        const due = list.filter((c:any) => !c.nextReview || new Date(c.nextReview) <= now);
        setInitialDueTotal(due.length);
      } catch { setInitialDueTotal(0); }
      await loadNext();
      fetchDailyStats();
    };
    init();
  }, []);
  return (
    <VStack align="stretch" spacing={6}>
      <Heading size="md">Sesja nauki</Heading>
      <VStack align='stretch' spacing={2}>
        <HStack fontSize='sm' color='gray.600' wrap='wrap'>
          <Text>Odpowiedziano: {answered}</Text>
          <Text>Unikalne: {uniqueReviewedIds.size}</Text>
          <Text>Poprawne: {correct}</Text>
          <Text>Skuteczność: {answered ? Math.round((correct/answered)*100) : 0}%</Text>
          {learningQueue.length>0 && <Text>Kolejka nauki: {learningQueue.length}</Text>}
          {initialDueTotal !== null && <Text>Pula startowa: {initialDueTotal}</Text>}
        </HStack>
        <Progress size='sm' value={initialDueTotal ? Math.min(uniqueReviewedIds.size / initialDueTotal * 100, 100) : 0} />
        {dailyStats && (
          <Box p={3} borderWidth='1px' borderRadius='md' bg='gray.50'>
            <HStack fontSize='xs' spacing={4} wrap='wrap'>
              <Text>Dzisiaj powtórki: {dailyStats.total}</Text>
              <Text>Poprawne (≥3): {dailyStats.correct}</Text>
              <Text>Skuteczność: {Math.round(dailyStats.accuracy*100)}%</Text>
            </HStack>
          </Box>
        )}
      </VStack>
  {/* Info box with next review removed per request */}
      {!current && <Text>Brak kart do powtórki teraz.</Text>}
      {current && (
        <Flex justify='center'>
          <Box p={6} borderWidth="1px" borderRadius="lg" bg="white" maxW='640px' w='100%' shadow='sm'>
            <VStack align='stretch' spacing={4}>
              <Box>
                <Text fontSize="xl" fontWeight="bold" mb={2} textAlign='center'>{current.front}</Text>
                {revealed && <Divider mb={4} />}
                {revealed && <Text mb={2} fontSize='md' textAlign='center'>{back}</Text>}
              </Box>
              {!revealed && <Button colorScheme="blue" onClick={reveal} alignSelf='center'>Pokaż odpowiedź</Button>}
              {revealed && (
                <VStack align="stretch" spacing={3}>
                  <Text fontSize='sm' color='gray.600' textAlign='center'>Oceń jak dobrze pamiętałeś:</Text>
                  <HStack justify='center' wrap='wrap'>
                    {[0,1,2,3,4,5].map(q => (
                      <Tooltip key={q} label={ratingLabel(q)}>
                        <Button size="sm" onClick={() => review(q)}>{q}</Button>
                      </Tooltip>
                    ))}
                  </HStack>
                  <RatingLegend />
                </VStack>
              )}
            </VStack>
          </Box>
        </Flex>
      )}
    </VStack>
  );
}

function ratingLabel(q: number) {
  switch(q){
    case 0: return '0 – Nie pamiętam w ogóle';
    case 1: return '1 – Prawie nic';
    case 2: return '2 – Dużo błędów / zgadywanie';
    case 3: return '3 – Prawie dobrze (niepewność)';
    case 4: return '4 – Dobrze, mały wysiłek';
    case 5: return '5 – Perfekcyjnie / natychmiast';
    default: return '';
  }
}

function RatingLegend(){
  const items = [0,1,2,3,4,5].map(i => ({ i, text: ratingLabel(i).split(' – ')[1] }));
  return (
    <Box fontSize='xs' color='gray.600' borderWidth='1px' borderRadius='md' p={3} bg='gray.50'>
      <Text mb={1} fontWeight='semibold'>Skala ocen:</Text>
      <VStack align='stretch' spacing={1}>
        {items.map(it => (
          <HStack key={it.i} spacing={2} align='flex-start'>
            <Tag size='sm' colorScheme='blue'><TagLabel>{it.i}</TagLabel></Tag>
            <Text>{it.text}</Text>
          </HStack>
        ))}
      </VStack>
    </Box>
  );
}
