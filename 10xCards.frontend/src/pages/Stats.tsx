import { Box, Heading, SimpleGrid, Stat, StatLabel, StatNumber, StatHelpText, VStack, Spinner, Text } from '@chakra-ui/react';
import { useEffect, useState } from 'react';
import { api } from '../services/api';

interface GenerationStats {
  sessions: number;
  proposed: number;
  accepted: number;
  acceptanceRate: number;
  flashcardsTotal: number;
  flashcardsAI: number;
  flashcardsManual: number;
  aiUsageRate: number;
}

export default function Stats() {
  const [data, setData] = useState<GenerationStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string|null>(null);
  useEffect(() => { (async () => {
    try { const d = await api.get('/stats/generation'); setData(d); }
    catch(e:any){ setError(e.message); }
    finally { setLoading(false); }
  })(); }, []);

  return (
    <VStack align='stretch' spacing={6}>
      <Heading size='md'>Statystyki</Heading>
      {loading && <Spinner />}
      {error && <Text color='red.500'>{error}</Text>}
      {data && !loading && (
        <SimpleGrid columns={{base:1, md:3}} spacing={6}>
          <StatBox label='Sesje generowania' value={data.sessions} help='Łączna liczba sesji AI' />
          <StatBox label='Proponowane fiszki' value={data.proposed} help='Suma wszystkich propozycji' />
          <StatBox label='Zaakceptowane fiszki' value={data.accepted} help='Łącznie dodane z AI' />
          <StatBox label='Współczynnik akceptacji' value={(data.acceptanceRate*100).toFixed(1) + '%'} help='Zaakceptowane / Proponowane' />
          <StatBox label='Fiszki razem' value={data.flashcardsTotal} help='Całkowita liczba fiszek' />
            <StatBox label='Fiszki AI' value={data.flashcardsAI} help='Dodane z generowania' />
            <StatBox label='Fiszki manualne' value={data.flashcardsManual} help='Dodane ręcznie' />
          <StatBox label='Udział AI' value={(data.aiUsageRate*100).toFixed(1) + '%'} help='AI / wszystkie fiszki' />
        </SimpleGrid>
      )}
    </VStack>
  );
}

function StatBox({ label, value, help }: { label:string; value:any; help?:string; }) {
  return (
    <Stat p={4} borderWidth='1px' borderRadius='lg' bg='white'>
      <StatLabel>{label}</StatLabel>
      <StatNumber>{value}</StatNumber>
      {help && <StatHelpText>{help}</StatHelpText>}
    </Stat>
  );
}
