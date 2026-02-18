import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, ThemeToggleComponent],
  templateUrl: './landing.html',
  styles: [`
    @keyframes float-slow {
      0%, 100% { transform: translate(0, 0) scale(1); }
      33% { transform: translate(30px, -20px) scale(1.05); }
      66% { transform: translate(-20px, 15px) scale(0.95); }
    }
    @keyframes float-slower {
      0%, 100% { transform: translate(0, 0) scale(1); }
      50% { transform: translate(-40px, 25px) scale(1.08); }
    }
    @keyframes float-slowest {
      0%, 100% { transform: translate(0, 0); }
      25% { transform: translate(20px, -30px); }
      75% { transform: translate(-30px, 10px); }
    }
    @keyframes gradient-x {
      0%, 100% { background-position: 0% 50%; }
      50% { background-position: 100% 50%; }
    }
    @keyframes reveal-up {
      from { opacity: 0; transform: translateY(32px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @keyframes pulse-ring {
      0% { transform: scale(1); opacity: 0.6; }
      100% { transform: scale(2.5); opacity: 0; }
    }
    @keyframes count-fade {
      from { opacity: 0; transform: translateY(12px) scale(0.96); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }

    .orb-1 { animation: float-slow 20s ease-in-out infinite; }
    .orb-2 { animation: float-slower 25s ease-in-out infinite; }
    .orb-3 { animation: float-slowest 30s ease-in-out infinite; }

    .gradient-text {
      background: linear-gradient(135deg, #818cf8 0%, #6366f1 25%, #14b8a6 50%, #818cf8 75%, #6366f1 100%);
      background-size: 300% 300%;
      animation: gradient-x 8s ease infinite;
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .reveal {
      animation: reveal-up 0.7s cubic-bezier(0.16, 1, 0.3, 1) both;
    }
    .reveal-d1 { animation-delay: 0.1s; }
    .reveal-d2 { animation-delay: 0.2s; }
    .reveal-d3 { animation-delay: 0.3s; }
    .reveal-d4 { animation-delay: 0.4s; }
    .reveal-d5 { animation-delay: 0.5s; }

    .stat-item {
      animation: count-fade 0.6s cubic-bezier(0.16, 1, 0.3, 1) both;
    }
    .stat-d1 { animation-delay: 0.6s; }
    .stat-d2 { animation-delay: 0.75s; }
    .stat-d3 { animation-delay: 0.9s; }
    .stat-d4 { animation-delay: 1.05s; }

    .pulse-dot::after {
      content: '';
      position: absolute;
      inset: 0;
      border-radius: 9999px;
      background: #6366f1;
      animation: pulse-ring 2s cubic-bezier(0, 0, 0.2, 1) infinite;
    }

    .card-glow {
      transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
    }
    .card-glow:hover {
      border-color: rgba(99, 102, 241, 0.3);
      box-shadow: 0 0 40px -8px rgba(99, 102, 241, 0.15), 0 20px 40px -12px rgba(0, 0, 0, 0.4);
      transform: translateY(-2px);
    }

    .pricing-glow {
      background: linear-gradient(135deg, #6366f1, #14b8a6, #6366f1);
      background-size: 200% 200%;
      animation: gradient-x 4s ease infinite;
    }
  `]
})
export class LandingComponent {
  features = [
    { icon: '&#9889;', title: 'Auto-Pipeline', desc: 'Upload any document and we automatically build the optimal chunking, embedding, and retrieval pipeline. Zero configuration required.', span: true },
    { icon: '&#128065;', title: 'Chunk Preview', desc: 'See exactly how your documents are split, embedded, and indexed. Full transparency into your RAG pipeline.', span: false },
    { icon: '&#128640;', title: 'One-Click Deploy', desc: 'Deploy your chatbot instantly with a shareable link. No infrastructure to manage.', span: false },
    { icon: '&#129513;', title: 'Embeddable Widget', desc: 'Drop a single script tag into any website. Your chatbot works everywhere your users are.', span: false },
    { icon: '&#9889;', title: 'Streaming Responses', desc: 'Real-time token-by-token streaming for a natural, conversational experience.', span: false },
    { icon: '&#128202;', title: 'Usage Analytics', desc: 'Track queries, token usage, and costs across all your projects with detailed dashboards.', span: true },
  ];

  steps = [
    { num: 1, title: 'Upload', desc: 'Drop your PDFs, DOCX, or text files into your project' },
    { num: 2, title: 'Configure', desc: 'Customize chunking strategy, embeddings, and prompts' },
    { num: 3, title: 'Deploy', desc: 'Get a chatbot link or embed widget — instant, zero DevOps' },
  ];

  stats = [
    { value: '10K+', label: 'Chatbots Built' },
    { value: '99.9%', label: 'Uptime SLA' },
    { value: '<2s', label: 'Avg Response' },
    { value: '50M+', label: 'Queries Served' },
  ];

  plans = [
    {
      name: 'Free',
      price: '$0',
      period: '/mo',
      cta: 'Start Free',
      ctaStyle: 'secondary',
      features: ['1 project', '10 documents', '100 queries/day', 'Community support'],
    },
    {
      name: 'Pro',
      price: '$29',
      period: '/mo',
      cta: 'Get Pro',
      ctaStyle: 'primary',
      popular: true,
      features: ['Unlimited projects', '500 documents', 'Unlimited queries', 'Priority support', 'Custom embeddings'],
    },
    {
      name: 'Enterprise',
      price: 'Custom',
      period: '',
      cta: 'Contact Sales',
      ctaStyle: 'secondary',
      features: ['Everything in Pro', 'SSO & SAML', 'Dedicated infra', 'SLA & 24/7 support', 'On-premise option'],
    },
  ];
}
