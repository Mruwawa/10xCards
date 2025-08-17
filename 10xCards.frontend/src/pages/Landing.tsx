import { Box, Button, Flex, Heading, Text, VStack, SimpleGrid, Stat, StatLabel, StatNumber, HStack, Link, Skeleton, SkeletonText } from '@chakra-ui/react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../state/auth';
import { useEffect, useState } from 'react';
import { api } from '../services/api';

interface StudyDailyStats { total:number; correct:number; accuracy:number; distribution:{quality:number;count:number}[] }
interface GenerationStats { flashcardsTotal:number; flashcardsAI:number; flashcardsManual:number; acceptanceRate:number; }

export default function Landing(){
  const { token } = useAuth();
  const navigate = useNavigate();
  const [gen,setGen] = useState<GenerationStats|null>(null);
  const [study,setStudy] = useState<StudyDailyStats|null>(null);
  const [loadingGen,setLoadingGen] = useState(false);
  const [loadingStudy,setLoadingStudy] = useState(false);
  useEffect(()=>{
    if(token){
      (async()=>{
        setLoadingGen(true); setLoadingStudy(true);
        try { const g = await api.get('/stats/generation'); setGen(g); } catch{} finally { setLoadingGen(false); }
        try { const s = await api.get('/stats/study/today'); setStudy(s); } catch{} finally { setLoadingStudy(false); }
      })();
    }
  },[token]);

  if(!token){
    return (
      <Flex direction='column' align='center' textAlign='center' maxW='840px' mx='auto' py={12} gap={12}>
        <VStack spacing={4}>
          <Heading size='2xl'>10xCards</Heading>
          <Text fontSize='xl' color='gray.600'>Superszybkie fiszki wspierane AI + inteligentne powtórki.</Text>
          <Text fontSize='md' color='gray.600' maxW='640px'>Wklej materiał, wygeneruj propozycje fiszek z pomocą AI, zatwierdź tylko najlepsze i ucz się w krótkich, zoptymalizowanych sesjach. Algorytm powtórek adaptuje się do Twojej pamięci, a błędne odpowiedzi wracają natychmiast w kolejce nauki.</Text>
          <HStack spacing={4} pt={4}>
            <Button colorScheme='blue' size='lg' onClick={()=>navigate('/register')}>Rozpocznij teraz</Button>
            <Button variant='outline' size='lg' onClick={()=>navigate('/login')}>Mam już konto</Button>
          </HStack>
        </VStack>
        <SimpleGrid columns={{base:1, md:3}} spacing={8} w='100%'>
          <ValueBox title='AI Generation' desc='Automatyczne propozycje kart z tekstu.' />
          <ValueBox title='Smart Powtórki' desc='Spaced repetition + natychmiastowy retry.' />
          <ValueBox title='Statystyki' desc='Śledź postęp i skuteczność.' />
        </SimpleGrid>
        <Box bg='white' p={8} rounded='lg' shadow='sm' borderWidth='1px' maxW='760px'>
          <Heading size='md' mb={4}>Jak to działa?</Heading>
          <VStack align='stretch' spacing={3} fontSize='sm' color='gray.700'>
            <Text>1. Wklejasz fragment materiału (np. artykuł, notatki).</Text>
            <Text>2. AI proponuje zestaw fiszek – edytujesz i akceptujesz najlepsze.</Text>
            <Text>3. Uczysz się – oceny 0-2 trafiają do szybkiej kolejki nauki, 3-5 planują kolejne powtórki.</Text>
            <Text>4. W statystykach widzisz ile już opanowałeś i jaki masz współczynnik poprawności.</Text>
          </VStack>
          <Button mt={6} colorScheme='blue' size='md' onClick={()=>navigate('/register')}>Zacznij darmowo</Button>
        </Box>
        <Text fontSize='xs' color='gray.500'>© {new Date().getFullYear()} 10xCards</Text>
      </Flex>
    );
  }

  return (
    <VStack align='stretch' spacing={8} maxW='1000px' mx='auto' py={8}>
      <Heading size='lg'>Witaj! Gotowy do nauki?</Heading>
      <HStack spacing={4} wrap='wrap'>
        <Button colorScheme='blue' onClick={()=>navigate('/study')}>Start sesji</Button>
        <Button variant='outline' onClick={()=>navigate('/generate')}>Generuj fiszki</Button>
        <Button variant='outline' onClick={()=>navigate('/flashcards')}>Moje fiszki</Button>
        <Button variant='outline' onClick={()=>navigate('/stats')}>Statystyki</Button>
      </HStack>
      <SimpleGrid columns={{base:1, md:4}} spacing={4}>
        {loadingGen ? <SkeletonStat /> : <StatBox label='Fiszki' value={gen?.flashcardsTotal ?? 0} />}
        {loadingGen ? <SkeletonStat /> : <StatBox label='AI %' value={gen && gen.flashcardsTotal ? Math.round(gen.flashcardsAI / gen.flashcardsTotal * 100) : 0} />}
        {loadingGen ? <SkeletonStat /> : <StatBox label='Akceptacja AI' value={gen ? Math.round(gen.acceptanceRate * 100) : 0} suffix='%' />}
        {loadingStudy ? <SkeletonStat /> : <StatBox label='Dzisiejsza skut.' value={study ? Math.round(study.accuracy*100) : 0} suffix='%' />}
      </SimpleGrid>
      {(loadingStudy || study) && (
        <Box p={4} bg='white' borderWidth='1px' rounded='md'>
          <Heading size='sm' mb={2}>Dzisiejsze powtórki</Heading>
          {loadingStudy && !study && <SkeletonText noOfLines={3} spacing={2} />}
          {study && !loadingStudy && (
            <HStack spacing={4} fontSize='sm' wrap='wrap'>
              <Text>Wszystkie: {study.total}</Text>
              <Text>Poprawne ≥3: {study.correct}</Text>
              <Text>Skuteczność: {Math.round(study.accuracy*100)}%</Text>
              <Text>Rozkład: {study.distribution.map(d=>`${d.quality}:${d.count}`).join(' ')}</Text>
            </HStack>
          )}
        </Box>
      )}
    </VStack>
  );
}

function ValueBox({title, desc}:{title:string;desc:string}){
  return (
    <Box p={5} bg='white' rounded='lg' shadow='sm' borderWidth='1px'>
      <Heading size='sm' mb={2}>{title}</Heading>
      <Text fontSize='sm' color='gray.600'>{desc}</Text>
    </Box>
  );
}

function StatBox({label,value,suffix}:{label:string;value:number|string; suffix?:string}){
  return (
    <Stat p={4} bg='white' rounded='md' borderWidth='1px'>
      <StatLabel fontSize='xs' textTransform='uppercase' color='gray.500'>{label}</StatLabel>
      <StatNumber fontSize='2xl'>{value}{suffix||''}</StatNumber>
    </Stat>
  );
}

function SkeletonStat(){
  return (
    <Stat p={4} bg='white' rounded='md' borderWidth='1px'>
      <StatLabel fontSize='xs' textTransform='uppercase' color='gray.500'><Skeleton h='10px' w='60%' /></StatLabel>
      <StatNumber fontSize='2xl'><Skeleton h='28px' w='70%' /></StatNumber>
    </Stat>
  );
}
