<template>
  <div>
    <h2>{{ service.title }}</h2>
    <p>{{ service.description }}</p>

    <template v-if="service.detailed_description">
      <h3>Details</h3>
      <div v-html="service.detailed_description"></div>
    </template>

    <template v-if="service.technologies && service.technologies.length > 0">
      <h3>Technologies</h3>
      <ul>
        <li v-for="(technology, index) in service.technologies" :key="index">
          {{ technology }}
        </li>
      </ul>
    </template>

    <template v-if="service.features && service.features.length > 0">
      <h3>Features</h3>
      <ul>
        <li v-for="(feature, index) in service.features" :key="index">
          {{ feature }}
        </li>
      </ul>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { Service } from '@/lib/clients/services/types';

interface ServiceContentProps {
  service: Service;
}

defineProps<ServiceContentProps>();
</script>
